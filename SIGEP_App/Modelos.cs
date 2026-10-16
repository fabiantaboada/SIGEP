using System;
using System.Globalization;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace SIGEP_App
{
    public class Configuracion
    {
        public string archivo;
        public string fecha_comunion;
        public string fecha_confirmacion;

        public Configuracion()
        {
            this.archivo = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "configuracion.json");
            this.fecha_comunion = "--/--";
            this.fecha_confirmacion = "--/--";
            this.cargar();
        }

        public void cargar()
        {
            if (File.Exists(this.archivo))
            {
                string contenido = File.ReadAllText(this.archivo, Encoding.UTF8);
                Match mCom = Regex.Match(contenido, "\"fecha_comunion\"\\s*:\\s*\"([^\"]+)\"");
                if (mCom.Success)
                {
                    this.fecha_comunion = mCom.Groups[1].Value;
                }
                Match mCon = Regex.Match(contenido, "\"fecha_confirmacion\"\\s*:\\s*\"([^\"]+)\"");
                if (mCon.Success)
                {
                    this.fecha_confirmacion = mCon.Groups[1].Value;
                }
            }
            else
            {
                this.guardar();
            }
        }

        public void guardar()
        {
            string json = "{\"fecha_comunion\": \"" + this.fecha_comunion + "\", \"fecha_confirmacion\": \"" + this.fecha_confirmacion + "\"}";
            File.WriteAllText(this.archivo, json, Encoding.UTF8);
        }
    }

    public class BaseDatos
    {
        public string archivo;
        private static readonly CultureInfo CulturaEs = new CultureInfo("es-PE");

        private int capacidad = 10;
        public int total_registros = 0;

        // 21 Arreglos paralelos unidimensionales puros
        public string[] origenes;
        public string[] dnis_prin;
        public string[] nombres;
        public string[] apellidos;
        public string[] dnis_sec;
        public string[] nombres_sec;
        public string[] apellidos_sec;
        public string[] generos;
        public string[] tipos;
        public string[] estados;
        public string[] tiene_padrinos;
        public string[] estado_padrinos;
        public string[] checks_mem;
        public string[] obs_auto;
        public string[] obs_adic;
        public string[] fechas_sac;
        public string[] libros;
        public string[] folios;
        public string[] asientos;
        public string[] pagos;
        public string[] deudas;

        public BaseDatos()
        {
            this.InicializarArreglos(10);
            this.archivo = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "registros_sigep.csv");
            this.cargar_datos();
        }

        private void InicializarArreglos(int capInicial)
        {
            this.capacidad = capInicial;
            this.total_registros = 0;

            this.origenes = new string[this.capacidad];
            this.dnis_prin = new string[this.capacidad];
            this.nombres = new string[this.capacidad];
            this.apellidos = new string[this.capacidad];
            this.dnis_sec = new string[this.capacidad];
            this.nombres_sec = new string[this.capacidad];
            this.apellidos_sec = new string[this.capacidad];
            this.generos = new string[this.capacidad];
            this.tipos = new string[this.capacidad];
            this.estados = new string[this.capacidad];
            this.tiene_padrinos = new string[this.capacidad];
            this.estado_padrinos = new string[this.capacidad];
            this.checks_mem = new string[this.capacidad];
            this.obs_auto = new string[this.capacidad];
            this.obs_adic = new string[this.capacidad];
            this.fechas_sac = new string[this.capacidad];
            this.libros = new string[this.capacidad];
            this.folios = new string[this.capacidad];
            this.asientos = new string[this.capacidad];
            this.pagos = new string[this.capacidad];
            this.deudas = new string[this.capacidad];
        }

        private static string[] AmpliarArregloTexto(string[] original, int cantidadActual, int nuevaCapacidad)
        {
            string[] nuevo = new string[nuevaCapacidad];
            for (int i = 0; i < cantidadActual; i++)
            {
                nuevo[i] = original[i];
            }
            return nuevo;
        }

        private void AsegurarCapacidad()
        {
            if (this.total_registros >= this.capacidad)
            {
                int nuevaCap = this.capacidad * 2;
                this.origenes = AmpliarArregloTexto(this.origenes, this.total_registros, nuevaCap);
                this.dnis_prin = AmpliarArregloTexto(this.dnis_prin, this.total_registros, nuevaCap);
                this.nombres = AmpliarArregloTexto(this.nombres, this.total_registros, nuevaCap);
                this.apellidos = AmpliarArregloTexto(this.apellidos, this.total_registros, nuevaCap);
                this.dnis_sec = AmpliarArregloTexto(this.dnis_sec, this.total_registros, nuevaCap);
                this.nombres_sec = AmpliarArregloTexto(this.nombres_sec, this.total_registros, nuevaCap);
                this.apellidos_sec = AmpliarArregloTexto(this.apellidos_sec, this.total_registros, nuevaCap);
                this.generos = AmpliarArregloTexto(this.generos, this.total_registros, nuevaCap);
                this.tipos = AmpliarArregloTexto(this.tipos, this.total_registros, nuevaCap);
                this.estados = AmpliarArregloTexto(this.estados, this.total_registros, nuevaCap);
                this.tiene_padrinos = AmpliarArregloTexto(this.tiene_padrinos, this.total_registros, nuevaCap);
                this.estado_padrinos = AmpliarArregloTexto(this.estado_padrinos, this.total_registros, nuevaCap);
                this.checks_mem = AmpliarArregloTexto(this.checks_mem, this.total_registros, nuevaCap);
                this.obs_auto = AmpliarArregloTexto(this.obs_auto, this.total_registros, nuevaCap);
                this.obs_adic = AmpliarArregloTexto(this.obs_adic, this.total_registros, nuevaCap);
                this.fechas_sac = AmpliarArregloTexto(this.fechas_sac, this.total_registros, nuevaCap);
                this.libros = AmpliarArregloTexto(this.libros, this.total_registros, nuevaCap);
                this.folios = AmpliarArregloTexto(this.folios, this.total_registros, nuevaCap);
                this.asientos = AmpliarArregloTexto(this.asientos, this.total_registros, nuevaCap);
                this.pagos = AmpliarArregloTexto(this.pagos, this.total_registros, nuevaCap);
                this.deudas = AmpliarArregloTexto(this.deudas, this.total_registros, nuevaCap);

                this.capacidad = nuevaCap;
            }
        }

        public static string A_MayusculasConTildes(string texto)
        {
            if (string.IsNullOrEmpty(texto))
            {
                return "";
            }
            return texto.Trim().ToUpper(CulturaEs);
        }

        public static string LimpiarTextoCelda(string valor)
        {
            if (string.IsNullOrEmpty(valor))
            {
                return "";
            }
            string limpio = valor.Replace("\r\n", " | ").Replace("\n", " | ").Replace("\r", " | ");
            return limpio.Trim();
        }

        public static string RestaurarSaltosLinea(string valor)
        {
            if (string.IsNullOrEmpty(valor))
            {
                return "";
            }
            return valor.Replace(" | ", "\r\n");
        }

        public static string EscaparCsv(string valor)
        {
            if (valor == null)
            {
                return "";
            }
            string v = LimpiarTextoCelda(valor);
            bool necesitaComillas = false;
            if (v.Contains(",") || v.Contains("\"") || v.Contains("\n") || v.Contains("\r"))
            {
                necesitaComillas = true;
            }
            if (necesitaComillas)
            {
                return "\"" + v.Replace("\"", "\"\"") + "\"";
            }
            return v;
        }

        private static string[][] AmpliarMatrizFilas(string[][] original, int cantidadActual, int nuevaCapacidad)
        {
            string[][] nueva = new string[nuevaCapacidad][];
            for (int i = 0; i < cantidadActual; i++)
            {
                nueva[i] = original[i];
            }
            return nueva;
        }

        public static string[][] LeerTodoCsv(string ruta)
        {
            int capFilas = 16;
            string[][] filasTemp = new string[capFilas][];
            int totalFilas = 0;

            string contenido = File.ReadAllText(ruta, Encoding.UTF8);
            string[] filaActual = new string[30];
            int colActual = 0;
            StringBuilder celdaActual = new StringBuilder();
            bool enComillas = false;

            for (int i = 0; i < contenido.Length; i++)
            {
                char c = contenido[i];
                if (enComillas)
                {
                    if (c == '\"')
                    {
                        if (i + 1 < contenido.Length && contenido[i + 1] == '\"')
                        {
                            celdaActual.Append('\"');
                            i++;
                        }
                        else
                        {
                            enComillas = false;
                        }
                    }
                    else
                    {
                        celdaActual.Append(c);
                    }
                }
                else
                {
                    if (c == '\"')
                    {
                        enComillas = true;
                    }
                    else if (c == ',')
                    {
                        if (colActual >= filaActual.Length)
                        {
                            filaActual = AmpliarArregloTexto(filaActual, colActual, filaActual.Length * 2);
                        }
                        filaActual[colActual] = celdaActual.ToString();
                        colActual++;
                        celdaActual.Clear();
                    }
                    else if (c == '\r')
                    {
                        continue;
                    }
                    else if (c == '\n')
                    {
                        if (colActual >= filaActual.Length)
                        {
                            filaActual = AmpliarArregloTexto(filaActual, colActual, filaActual.Length * 2);
                        }
                        filaActual[colActual] = celdaActual.ToString();
                        colActual++;
                        celdaActual.Clear();

                        if (colActual > 1 || (colActual == 1 && filaActual[0].Trim() != ""))
                        {
                            string[] filaDefinitiva = new string[colActual];
                            for (int k = 0; k < colActual; k++)
                            {
                                filaDefinitiva[k] = filaActual[k];
                            }
                            if (totalFilas >= capFilas)
                            {
                                capFilas = capFilas * 2;
                                filasTemp = AmpliarMatrizFilas(filasTemp, totalFilas, capFilas);
                            }
                            filasTemp[totalFilas] = filaDefinitiva;
                            totalFilas++;
                        }
                        colActual = 0;
                    }
                    else
                    {
                        celdaActual.Append(c);
                    }
                }
            }

            if (celdaActual.Length > 0 || colActual > 0)
            {
                if (colActual >= filaActual.Length)
                {
                    filaActual = AmpliarArregloTexto(filaActual, colActual, filaActual.Length * 2);
                }
                filaActual[colActual] = celdaActual.ToString();
                colActual++;

                if (colActual > 1 || (colActual == 1 && filaActual[0].Trim() != ""))
                {
                    string[] filaDefinitiva = new string[colActual];
                    for (int k = 0; k < colActual; k++)
                    {
                        filaDefinitiva[k] = filaActual[k];
                    }
                    if (totalFilas >= capFilas)
                    {
                        capFilas = capFilas * 2;
                        filasTemp = AmpliarMatrizFilas(filasTemp, totalFilas, capFilas);
                    }
                    filasTemp[totalFilas] = filaDefinitiva;
                    totalFilas++;
                }
            }

            string[][] resultado = new string[totalFilas][];
            for (int i = 0; i < totalFilas; i++)
            {
                resultado[i] = filasTemp[i];
            }
            return resultado;
        }

        public void crear_cabeceras()
        {
            string cabecera = "ORIGEN,DNI_TITULAR,NOMBRES_TITULAR,APELLIDOS_TITULAR,DNI_CONYUGE,NOMBRES_CONYUGE,APELLIDOS_CONYUGE,GENERO,SACRAMENTO,ESTADO,TIENE_PADRINOS,ESTADO_CIVIL_PADRINOS,CHECKS,OBS_AUTO,OBS_ADIC,FECHA_SAC,LIBRO,FOLIO,ASIENTO,PAGO,DEUDA\r\n";
            File.WriteAllText(this.archivo, cabecera, Encoding.UTF8);
        }

        public void limpiar_arreglos()
        {
            this.InicializarArreglos(10);
        }

        public static void SepararNombreCompleto(string completo, out string nom, out string ape)
        {
            nom = "";
            ape = "";
            if (string.IsNullOrWhiteSpace(completo) || completo == "---")
            {
                nom = "---";
                ape = "---";
                return;
            }
            string[] partes = A_MayusculasConTildes(completo).Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (partes.Length >= 3)
            {
                ape = partes[partes.Length - 2] + " " + partes[partes.Length - 1];
                nom = string.Join(" ", partes, 0, partes.Length - 2);
            }
            else if (partes.Length == 2)
            {
                nom = partes[0];
                ape = partes[1];
            }
            else
            {
                nom = A_MayusculasConTildes(completo);
                ape = "-";
            }
        }

        public void cargar_datos()
        {
            if (!File.Exists(this.archivo))
            {
                this.crear_cabeceras();
            }
            else
            {
                this.limpiar_arreglos();
                string[][] filas = LeerTodoCsv(this.archivo);
                bool migrarFormatoAntiguo = false;

                for (int i = 1; i < filas.Length; i++)
                {
                    string[] fila = filas[i];
                    if (fila.Length == 21)
                    {
                        this.AsegurarCapacidad();
                        int pos = this.total_registros;
                        this.origenes[pos] = A_MayusculasConTildes(fila[0]);
                        this.dnis_prin[pos] = fila[1].Trim();
                        this.nombres[pos] = A_MayusculasConTildes(fila[2]);
                        this.apellidos[pos] = A_MayusculasConTildes(fila[3]);
                        this.dnis_sec[pos] = fila[4].Trim();
                        this.nombres_sec[pos] = A_MayusculasConTildes(fila[5]);
                        this.apellidos_sec[pos] = A_MayusculasConTildes(fila[6]);
                        this.generos[pos] = fila[7].Trim();
                        string sac = (fila[8].Trim() == "Matrimonio Regular") ? "Matrimonio" : fila[8].Trim();
                        this.tipos[pos] = sac;
                        this.estados[pos] = fila[9].Trim();
                        this.tiene_padrinos[pos] = fila[10].Trim();
                        this.estado_padrinos[pos] = fila[11].Trim();
                        this.checks_mem[pos] = fila[12].Trim();
                        this.obs_auto[pos] = RestaurarSaltosLinea(fila[13]);
                        this.obs_adic[pos] = RestaurarSaltosLinea(fila[14]);
                        this.fechas_sac[pos] = fila[15].Trim();
                        this.libros[pos] = fila[16].Trim();
                        this.folios[pos] = fila[17].Trim();
                        this.asientos[pos] = fila[18].Trim();
                        this.pagos[pos] = fila[19].Trim();
                        this.deudas[pos] = fila[20].Trim();
                        this.total_registros++;
                    }
                    else if (fila.Length == 17)
                    {
                        migrarFormatoAntiguo = true;
                        this.AsegurarCapacidad();
                        int pos = this.total_registros;
                        bool esHist = fila[10].StartsWith("HIST|");
                        this.origenes[pos] = esHist ? "ANTIGUO" : "TRAMITE";
                        this.dnis_prin[pos] = fila[0].Trim();

                        string n1, a1, n2, a2;
                        if (esHist)
                        {
                            string[] pH = fila[10].Split('|');
                            n1 = pH.Length > 1 ? A_MayusculasConTildes(pH[1]) : A_MayusculasConTildes(fila[2]);
                            a1 = pH.Length > 2 ? A_MayusculasConTildes(pH[2]) : "";
                            n2 = pH.Length > 3 ? A_MayusculasConTildes(pH[3]) : "---";
                            a2 = pH.Length > 4 ? A_MayusculasConTildes(pH[4]) : "---";
                        }
                        else
                        {
                            SepararNombreCompleto(fila[2], out n1, out a1);
                            SepararNombreCompleto(fila[3], out n2, out a2);
                        }

                        string sac = (fila[6].Trim() == "Matrimonio Regular") ? "Matrimonio" : fila[6].Trim();
                        this.nombres[pos] = n1;
                        this.apellidos[pos] = a1;
                        this.dnis_sec[pos] = (sac == "Matrimonio") ? fila[1].Trim() : "---";
                        this.nombres_sec[pos] = (sac == "Matrimonio") ? n2 : "---";
                        this.apellidos_sec[pos] = (sac == "Matrimonio") ? a2 : "---";
                        this.generos[pos] = fila[5].Trim();
                        this.tipos[pos] = sac;
                        this.estados[pos] = fila[7].Trim();
                        this.tiene_padrinos[pos] = "NO";
                        this.estado_padrinos[pos] = "---";
                        this.checks_mem[pos] = esHist ? "1-1-1-1-1-1-1-1-1" : fila[10].Replace(",", "-");
                        this.obs_auto[pos] = RestaurarSaltosLinea(fila[8]);
                        this.obs_adic[pos] = RestaurarSaltosLinea(fila[9]);
                        this.fechas_sac[pos] = fila[11].Trim();
                        this.libros[pos] = fila[12].Trim();
                        this.folios[pos] = fila[13].Trim();
                        this.asientos[pos] = fila[14].Trim();
                        this.pagos[pos] = fila[15].Trim();
                        this.deudas[pos] = fila[16].Trim();
                        this.total_registros++;
                    }
                }

                if (migrarFormatoAntiguo)
                {
                    this.guardar_datos();
                }
            }
        }

        public void guardar_datos()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("ORIGEN,DNI_TITULAR,NOMBRES_TITULAR,APELLIDOS_TITULAR,DNI_CONYUGE,NOMBRES_CONYUGE,APELLIDOS_CONYUGE,GENERO,SACRAMENTO,ESTADO,TIENE_PADRINOS,ESTADO_CIVIL_PADRINOS,CHECKS,OBS_AUTO,OBS_ADIC,FECHA_SAC,LIBRO,FOLIO,ASIENTO,PAGO,DEUDA");

            for (int i = 0; i < this.total_registros; i++)
            {
                string linea = string.Join(",", new string[] {
                    EscaparCsv(this.origenes[i]),
                    EscaparCsv(this.dnis_prin[i]),
                    EscaparCsv(A_MayusculasConTildes(this.nombres[i])),
                    EscaparCsv(A_MayusculasConTildes(this.apellidos[i])),
                    EscaparCsv(this.dnis_sec[i]),
                    EscaparCsv(A_MayusculasConTildes(this.nombres_sec[i])),
                    EscaparCsv(A_MayusculasConTildes(this.apellidos_sec[i])),
                    EscaparCsv(this.generos[i]),
                    EscaparCsv(this.tipos[i]),
                    EscaparCsv(this.estados[i]),
                    EscaparCsv(this.tiene_padrinos[i]),
                    EscaparCsv(this.estado_padrinos[i]),
                    EscaparCsv(this.checks_mem[i]),
                    EscaparCsv(this.obs_auto[i]),
                    EscaparCsv(this.obs_adic[i]),
                    EscaparCsv(this.fechas_sac[i]),
                    EscaparCsv(this.libros[i]),
                    EscaparCsv(this.folios[i]),
                    EscaparCsv(this.asientos[i]),
                    EscaparCsv(this.pagos[i]),
                    EscaparCsv(this.deudas[i])
                });
                sb.AppendLine(linea);
            }
            File.WriteAllText(this.archivo, sb.ToString(), Encoding.UTF8);
        }

        public void registrar(string origen, string d1, string nom1, string ape1, string d2, string nom2, string ape2, string gen, string tipo, string est, string t_pad, string est_pad, string chks, string o_auto, string o_adic, string fec, string lib, string fol, string asi, string pag, string deu)
        {
            this.AsegurarCapacidad();

            int pos = this.total_registros;
            this.origenes[pos] = A_MayusculasConTildes(origen);
            this.dnis_prin[pos] = d1;
            this.nombres[pos] = A_MayusculasConTildes(nom1);
            this.apellidos[pos] = A_MayusculasConTildes(ape1);
            this.dnis_sec[pos] = d2;
            this.nombres_sec[pos] = A_MayusculasConTildes(nom2);
            this.apellidos_sec[pos] = A_MayusculasConTildes(ape2);
            this.generos[pos] = gen;
            this.tipos[pos] = tipo;
            this.estados[pos] = est;
            this.tiene_padrinos[pos] = t_pad;
            this.estado_padrinos[pos] = est_pad;
            this.checks_mem[pos] = chks;
            this.obs_auto[pos] = o_auto;
            this.obs_adic[pos] = o_adic;
            this.fechas_sac[pos] = fec;
            this.libros[pos] = lib;
            this.folios[pos] = fol;
            this.asientos[pos] = asi;
            this.pagos[pos] = pag;
            this.deudas[pos] = deu;

            this.total_registros++;
            this.guardar_datos();

            ApiReniec.LimpiarCache();
        }

        public void actualizar_completo(int idx, string d1, string nom1, string ape1, string d2, string nom2, string ape2, string gen, string tipo, string est, string t_pad, string est_pad, string chks, string o_auto, string o_adic, string fec, string lib, string fol, string asi, string pag, string deu)
        {
            if (idx < 0 || idx >= this.total_registros)
            {
                return;
            }

            this.dnis_prin[idx] = d1;
            this.nombres[idx] = A_MayusculasConTildes(nom1);
            this.apellidos[idx] = A_MayusculasConTildes(ape1);
            this.dnis_sec[idx] = d2;
            this.nombres_sec[idx] = A_MayusculasConTildes(nom2);
            this.apellidos_sec[idx] = A_MayusculasConTildes(ape2);
            this.generos[idx] = gen;
            this.tipos[idx] = tipo;
            this.estados[idx] = est;
            this.tiene_padrinos[idx] = t_pad;
            this.estado_padrinos[idx] = est_pad;
            this.checks_mem[idx] = chks;
            this.obs_auto[idx] = o_auto;
            this.obs_adic[idx] = o_adic;
            this.fechas_sac[idx] = fec;
            this.libros[idx] = lib;
            this.folios[idx] = fol;
            this.asientos[idx] = asi;
            this.pagos[idx] = pag;
            this.deudas[idx] = deu;
            this.guardar_datos();

            ApiReniec.LimpiarCache();
        }

        public void eliminar(int idx)
        {
            if (idx < 0 || idx >= this.total_registros)
            {
                return;
            }

            for (int i = idx; i < this.total_registros - 1; i++)
            {
                this.origenes[i] = this.origenes[i + 1];
                this.dnis_prin[i] = this.dnis_prin[i + 1];
                this.nombres[i] = this.nombres[i + 1];
                this.apellidos[i] = this.apellidos[i + 1];
                this.dnis_sec[i] = this.dnis_sec[i + 1];
                this.nombres_sec[i] = this.nombres_sec[i + 1];
                this.apellidos_sec[i] = this.apellidos_sec[i + 1];
                this.generos[i] = this.generos[i + 1];
                this.tipos[i] = this.tipos[i + 1];
                this.estados[i] = this.estados[i + 1];
                this.tiene_padrinos[i] = this.tiene_padrinos[i + 1];
                this.estado_padrinos[i] = this.estado_padrinos[i + 1];
                this.checks_mem[i] = this.checks_mem[i + 1];
                this.obs_auto[i] = this.obs_auto[i + 1];
                this.obs_adic[i] = this.obs_adic[i + 1];
                this.fechas_sac[i] = this.fechas_sac[i + 1];
                this.libros[i] = this.libros[i + 1];
                this.folios[i] = this.folios[i + 1];
                this.asientos[i] = this.asientos[i + 1];
                this.pagos[i] = this.pagos[i + 1];
                this.deudas[i] = this.deudas[i + 1];
            }

            this.total_registros--;
            this.guardar_datos();
        }

        public static string NormalizarSinTildes(string texto)
        {
            if (string.IsNullOrEmpty(texto))
            {
                return "";
            }
            string s = A_MayusculasConTildes(texto);
            s = s.Replace('Á', 'A').Replace('É', 'E').Replace('Í', 'I').Replace('Ó', 'O').Replace('Ú', 'U').Replace('Ü', 'U');
            return s;
        }

        public int[] buscar_indices_por_dni(string dni)
        {
            return this.buscar_indices_por_dni(dni, false);
        }

        public int[] buscar_indices_por_dni(string dni, bool soloTramites)
        {
            int[] temp = new int[this.total_registros];
            int contador = 0;

            for (int i = 0; i < this.total_registros; i++)
            {
                if (soloTramites && this.origenes[i] == "ANTIGUO")
                {
                    continue;
                }
                if (this.dnis_prin[i] == dni && dni != "SIN DNI" && dni != "---")
                {
                    temp[contador] = i;
                    contador++;
                }
                else if (this.dnis_sec[i] == dni && dni != "" && dni != "---" && dni != "SIN DNI")
                {
                    temp[contador] = i;
                    contador++;
                }
            }

            int[] resultados = new int[contador];
            for (int j = 0; j < contador; j++)
            {
                resultados[j] = temp[j];
            }
            return resultados;
        }

        public int[] buscar_indices_por_apellidos(string apellidoBuscado)
        {
            return this.buscar_indices_por_apellidos(apellidoBuscado, false);
        }

        public int[] buscar_indices_por_apellidos(string apellidoBuscado, bool soloTramites)
        {
            string busqNorm = NormalizarSinTildes(apellidoBuscado.Trim());
            if (busqNorm == "")
            {
                return new int[0];
            }

            int[] temp = new int[this.total_registros];
            int contador = 0;

            for (int i = 0; i < this.total_registros; i++)
            {
                if (soloTramites && this.origenes[i] == "ANTIGUO")
                {
                    continue;
                }
                string ape1Norm = NormalizarSinTildes(this.apellidos[i]);
                string ape2Norm = NormalizarSinTildes(this.apellidos_sec[i]);

                if (ape1Norm.Contains(busqNorm) || (ape2Norm != "---" && ape2Norm.Contains(busqNorm)))
                {
                    temp[contador] = i;
                    contador++;
                }
            }

            int[] resultados = new int[contador];
            for (int j = 0; j < contador; j++)
            {
                resultados[j] = temp[j];
            }
            return resultados;
        }

        public string ObtenerNombreCompletoTitular(int i)
        {
            return (this.nombres[i] + " " + this.apellidos[i]).Trim();
        }

        public string ObtenerNombreCompletoConyuge(int i)
        {
            if (this.nombres_sec[i] == "---" || this.nombres_sec[i] == "")
            {
                return "";
            }
            return (this.nombres_sec[i] + " " + this.apellidos_sec[i]).Trim();
        }

        private void _swap(int i, int j)
        {
            string temp = this.origenes[i]; this.origenes[i] = this.origenes[j]; this.origenes[j] = temp;
            temp = this.dnis_prin[i]; this.dnis_prin[i] = this.dnis_prin[j]; this.dnis_prin[j] = temp;
            temp = this.nombres[i]; this.nombres[i] = this.nombres[j]; this.nombres[j] = temp;
            temp = this.apellidos[i]; this.apellidos[i] = this.apellidos[j]; this.apellidos[j] = temp;
            temp = this.dnis_sec[i]; this.dnis_sec[i] = this.dnis_sec[j]; this.dnis_sec[j] = temp;
            temp = this.nombres_sec[i]; this.nombres_sec[i] = this.nombres_sec[j]; this.nombres_sec[j] = temp;
            temp = this.apellidos_sec[i]; this.apellidos_sec[i] = this.apellidos_sec[j]; this.apellidos_sec[j] = temp;
            temp = this.generos[i]; this.generos[i] = this.generos[j]; this.generos[j] = temp;
            temp = this.tipos[i]; this.tipos[i] = this.tipos[j]; this.tipos[j] = temp;
            temp = this.estados[i]; this.estados[i] = this.estados[j]; this.estados[j] = temp;
            temp = this.tiene_padrinos[i]; this.tiene_padrinos[i] = this.tiene_padrinos[j]; this.tiene_padrinos[j] = temp;
            temp = this.estado_padrinos[i]; this.estado_padrinos[i] = this.estado_padrinos[j]; this.estado_padrinos[j] = temp;
            temp = this.checks_mem[i]; this.checks_mem[i] = this.checks_mem[j]; this.checks_mem[j] = temp;
            temp = this.obs_auto[i]; this.obs_auto[i] = this.obs_auto[j]; this.obs_auto[j] = temp;
            temp = this.obs_adic[i]; this.obs_adic[i] = this.obs_adic[j]; this.obs_adic[j] = temp;
            temp = this.fechas_sac[i]; this.fechas_sac[i] = this.fechas_sac[j]; this.fechas_sac[j] = temp;
            temp = this.libros[i]; this.libros[i] = this.libros[j]; this.libros[j] = temp;
            temp = this.folios[i]; this.folios[i] = this.folios[j]; this.folios[j] = temp;
            temp = this.asientos[i]; this.asientos[i] = this.asientos[j]; this.asientos[j] = temp;
            temp = this.pagos[i]; this.pagos[i] = this.pagos[j]; this.pagos[j] = temp;
            temp = this.deudas[i]; this.deudas[i] = this.deudas[j]; this.deudas[j] = temp;
        }

        public void ordenar_bubble_asc()
        {
            for (int i = 0; i < this.total_registros - 1; i++)
            {
                for (int j = 0; j < this.total_registros - i - 1; j++)
                {
                    string clave1 = A_MayusculasConTildes(this.apellidos[j] + " " + this.nombres[j]);
                    string clave2 = A_MayusculasConTildes(this.apellidos[j + 1] + " " + this.nombres[j + 1]);
                    if (string.Compare(clave1, clave2, StringComparison.Ordinal) > 0)
                    {
                        this._swap(j, j + 1);
                    }
                }
            }
            this.guardar_datos();
        }

        public void ordenar_selection_desc()
        {
            for (int i = 0; i < this.total_registros; i++)
            {
                int min_pos = i;
                for (int j = i + 1; j < this.total_registros; j++)
                {
                    string claveJ = A_MayusculasConTildes(this.apellidos[j] + " " + this.nombres[j]);
                    string claveMin = A_MayusculasConTildes(this.apellidos[min_pos] + " " + this.nombres[min_pos]);
                    if (string.Compare(claveJ, claveMin, StringComparison.Ordinal) > 0)
                    {
                        min_pos = j;
                    }
                }
                if (min_pos != i)
                {
                    this._swap(i, min_pos);
                }
            }
            this.guardar_datos();
        }
    }

    public static class ApiReniec
    {
        private static int capCache = 10;
        private static string[] cacheDnis = new string[10];
        private static string[] cacheNombres = new string[10];
        private static string[] cacheApellidos = new string[10];
        private static int totalCache = 0;

        public static void LimpiarCache()
        {
            capCache = 10;
            cacheDnis = new string[10];
            cacheNombres = new string[10];
            cacheApellidos = new string[10];
            totalCache = 0;
        }

        private static void AsegurarCapacidadCache()
        {
            if (totalCache >= capCache)
            {
                int nuevaCap = capCache * 2;
                string[] nuevoDnis = new string[nuevaCap];
                string[] nuevoNoms = new string[nuevaCap];
                string[] nuevoApes = new string[nuevaCap];

                for (int i = 0; i < totalCache; i++)
                {
                    nuevoDnis[i] = cacheDnis[i];
                    nuevoNoms[i] = cacheNombres[i];
                    nuevoApes[i] = cacheApellidos[i];
                }

                cacheDnis = nuevoDnis;
                cacheNombres = nuevoNoms;
                cacheApellidos = nuevoApes;
                capCache = nuevaCap;
            }
        }

        private static int BuscarEnCache(string dni)
        {
            for (int i = 0; i < totalCache; i++)
            {
                if (cacheDnis[i] == dni)
                {
                    return i;
                }
            }
            return -1;
        }

        private static string DecodificarTextoJson(string raw)
        {
            if (string.IsNullOrEmpty(raw))
            {
                return "";
            }
            try
            {
                string limpio = BaseDatos.A_MayusculasConTildes(Regex.Unescape(raw));
                return Regex.Replace(limpio, @"[^A-ZÁÉÍÓÚÑÜ ]", "");
            }
            catch
            {
                string limpio = BaseDatos.A_MayusculasConTildes(raw);
                return Regex.Replace(limpio, @"[^A-ZÁÉÍÓÚÑÜ ]", "");
            }
        }

        public static bool consultar_dni_separado(string dni, out string nombres, out string apellidos)
        {
            nombres = "";
            apellidos = "";

            if (string.IsNullOrWhiteSpace(dni) || dni.Length != 8)
            {
                return false;
            }

            int posCache = BuscarEnCache(dni);
            if (posCache != -1)
            {
                nombres = cacheNombres[posCache];
                apellidos = cacheApellidos[posCache];
                return true;
            }

            for (int intento = 0; intento < 2; intento++)
            {
                try
                {
                    ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                    string url = "https://api.apis.net.pe/v1/dni?numero=" + dni;
                    HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                    request.Timeout = 4500;
                    request.Method = "GET";
                    request.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) SIGEP/1.0";
                    request.Accept = "application/json";
                    request.KeepAlive = false;

                    using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                    {
                        if (response.StatusCode == HttpStatusCode.OK)
                        {
                            using (StreamReader reader = new StreamReader(response.GetResponseStream(), Encoding.UTF8))
                            {
                                string json = reader.ReadToEnd();
                                Match mNom = Regex.Match(json, "\"nombres\"\\s*:\\s*\"([^\"]+)\"");
                                Match mPat = Regex.Match(json, "\"apellidoPaterno\"\\s*:\\s*\"([^\"]+)\"");
                                Match mMat = Regex.Match(json, "\"apellidoMaterno\"\\s*:\\s*\"([^\"]+)\"");

                                if (mNom.Success && !string.IsNullOrWhiteSpace(mNom.Groups[1].Value))
                                {
                                    nombres = DecodificarTextoJson(mNom.Groups[1].Value);
                                    string pat = mPat.Success ? DecodificarTextoJson(mPat.Groups[1].Value) : "";
                                    string mat = mMat.Success ? DecodificarTextoJson(mMat.Groups[1].Value) : "";
                                    apellidos = BaseDatos.A_MayusculasConTildes(pat + " " + mat);

                                    AsegurarCapacidadCache();
                                    cacheDnis[totalCache] = dni;
                                    cacheNombres[totalCache] = nombres;
                                    cacheApellidos[totalCache] = apellidos;
                                    totalCache++;
                                    return true;
                                }
                            }
                        }
                    }
                }
                catch
                {
                    System.Threading.Thread.Sleep(250);
                }
            }
            return false;
        }
    }
}