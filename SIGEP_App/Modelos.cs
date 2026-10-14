using System;
using System.Collections.Generic;
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

        // Arreglos paralelos separados por celdas individuales (21 columnas)
        public List<string> origenes = new List<string>();
        public List<string> dnis_prin = new List<string>();
        public List<string> nombres = new List<string>();
        public List<string> apellidos = new List<string>();
        public List<string> dnis_sec = new List<string>();
        public List<string> nombres_sec = new List<string>();
        public List<string> apellidos_sec = new List<string>();
        public List<string> generos = new List<string>();
        public List<string> tipos = new List<string>();
        public List<string> estados = new List<string>();
        public List<string> tiene_padrinos = new List<string>();
        public List<string> estado_padrinos = new List<string>();
        public List<string> checks_mem = new List<string>();
        public List<string> obs_auto = new List<string>();
        public List<string> obs_adic = new List<string>();
        public List<string> fechas_sac = new List<string>();
        public List<string> libros = new List<string>();
        public List<string> folios = new List<string>();
        public List<string> asientos = new List<string>();
        public List<string> pagos = new List<string>();
        public List<string> deudas = new List<string>();

        public BaseDatos()
        {
            this.archivo = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "registros_sigep.csv");
            this.cargar_datos();
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

        public static List<string[]> LeerTodoCsv(string ruta)
        {
            List<string[]> filas = new List<string[]>();
            string contenido = File.ReadAllText(ruta, Encoding.UTF8);
            List<string> filaActual = new List<string>();
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
                        filaActual.Add(celdaActual.ToString());
                        celdaActual.Clear();
                    }
                    else if (c == '\r')
                    {
                        continue;
                    }
                    else if (c == '\n')
                    {
                        filaActual.Add(celdaActual.ToString());
                        celdaActual.Clear();
                        if (filaActual.Count > 1 || (filaActual.Count == 1 && filaActual[0].Trim() != ""))
                        {
                            filas.Add(filaActual.ToArray());
                        }
                        filaActual.Clear();
                    }
                    else
                    {
                        celdaActual.Append(c);
                    }
                }
            }

            if (celdaActual.Length > 0 || filaActual.Count > 0)
            {
                filaActual.Add(celdaActual.ToString());
                if (filaActual.Count > 1 || (filaActual.Count == 1 && filaActual[0].Trim() != ""))
                {
                    filas.Add(filaActual.ToArray());
                }
            }

            return filas;
        }

        public void crear_cabeceras()
        {
            string cabecera = "ORIGEN,DNI_TITULAR,NOMBRES_TITULAR,APELLIDOS_TITULAR,DNI_CONYUGE,NOMBRES_CONYUGE,APELLIDOS_CONYUGE,GENERO,SACRAMENTO,ESTADO,TIENE_PADRINOS,ESTADO_CIVIL_PADRINOS,CHECKS,OBS_AUTO,OBS_ADIC,FECHA_SAC,LIBRO,FOLIO,ASIENTO,PAGO,DEUDA\r\n";
            File.WriteAllText(this.archivo, cabecera, Encoding.UTF8);
        }

        public void limpiar_arreglos()
        {
            this.origenes.Clear();
            this.dnis_prin.Clear();
            this.nombres.Clear();
            this.apellidos.Clear();
            this.dnis_sec.Clear();
            this.nombres_sec.Clear();
            this.apellidos_sec.Clear();
            this.generos.Clear();
            this.tipos.Clear();
            this.estados.Clear();
            this.tiene_padrinos.Clear();
            this.estado_padrinos.Clear();
            this.checks_mem.Clear();
            this.obs_auto.Clear();
            this.obs_adic.Clear();
            this.fechas_sac.Clear();
            this.libros.Clear();
            this.folios.Clear();
            this.asientos.Clear();
            this.pagos.Clear();
            this.deudas.Clear();
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
                List<string[]> filas = LeerTodoCsv(this.archivo);
                bool migrarFormatoAntiguo = false;

                for (int i = 1; i < filas.Count; i++)
                {
                    string[] fila = filas[i];
                    if (fila.Length == 21)
                    {
                        this.origenes.Add(A_MayusculasConTildes(fila[0]));
                        this.dnis_prin.Add(fila[1].Trim());
                        this.nombres.Add(A_MayusculasConTildes(fila[2]));
                        this.apellidos.Add(A_MayusculasConTildes(fila[3]));
                        this.dnis_sec.Add(fila[4].Trim());
                        this.nombres_sec.Add(A_MayusculasConTildes(fila[5]));
                        this.apellidos_sec.Add(A_MayusculasConTildes(fila[6]));
                        this.generos.Add(fila[7].Trim());
                        string sac = (fila[8].Trim() == "Matrimonio Regular") ? "Matrimonio" : fila[8].Trim();
                        this.tipos.Add(sac);
                        this.estados.Add(fila[9].Trim());
                        this.tiene_padrinos.Add(fila[10].Trim());
                        this.estado_padrinos.Add(fila[11].Trim());
                        this.checks_mem.Add(fila[12].Trim());
                        this.obs_auto.Add(RestaurarSaltosLinea(fila[13]));
                        this.obs_adic.Add(RestaurarSaltosLinea(fila[14]));
                        this.fechas_sac.Add(fila[15].Trim());
                        this.libros.Add(fila[16].Trim());
                        this.folios.Add(fila[17].Trim());
                        this.asientos.Add(fila[18].Trim());
                        this.pagos.Add(fila[19].Trim());
                        this.deudas.Add(fila[20].Trim());
                    }
                    else if (fila.Length == 17)
                    {
                        migrarFormatoAntiguo = true;
                        bool esHist = fila[10].StartsWith("HIST|");
                        this.origenes.Add(esHist ? "ANTIGUO" : "TRAMITE");
                        this.dnis_prin.Add(fila[0].Trim());

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
                        this.nombres.Add(n1);
                        this.apellidos.Add(a1);
                        this.dnis_sec.Add(sac == "Matrimonio" ? fila[1].Trim() : "---");
                        this.nombres_sec.Add(sac == "Matrimonio" ? n2 : "---");
                        this.apellidos_sec.Add(sac == "Matrimonio" ? a2 : "---");
                        this.generos.Add(fila[5].Trim());
                        this.tipos.Add(sac);
                        this.estados.Add(fila[7].Trim());
                        this.tiene_padrinos.Add("NO");
                        this.estado_padrinos.Add("---");
                        this.checks_mem.Add(esHist ? "1-1-1" : fila[10].Replace(",", "-"));
                        this.obs_auto.Add(RestaurarSaltosLinea(fila[8]));
                        this.obs_adic.Add(RestaurarSaltosLinea(fila[9]));
                        this.fechas_sac.Add(fila[11].Trim());
                        this.libros.Add(fila[12].Trim());
                        this.folios.Add(fila[13].Trim());
                        this.asientos.Add(fila[14].Trim());
                        this.pagos.Add(fila[15].Trim());
                        this.deudas.Add(fila[16].Trim());
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
            int length = this.dnis_prin.Count;
            for (int i = 0; i < length; i++)
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
            this.origenes.Add(A_MayusculasConTildes(origen));
            this.dnis_prin.Add(d1);
            this.nombres.Add(A_MayusculasConTildes(nom1));
            this.apellidos.Add(A_MayusculasConTildes(ape1));
            this.dnis_sec.Add(d2);
            this.nombres_sec.Add(A_MayusculasConTildes(nom2));
            this.apellidos_sec.Add(A_MayusculasConTildes(ape2));
            this.generos.Add(gen);
            this.tipos.Add(tipo);
            this.estados.Add(est);
            this.tiene_padrinos.Add(t_pad);
            this.estado_padrinos.Add(est_pad);
            this.checks_mem.Add(chks);
            this.obs_auto.Add(o_auto);
            this.obs_adic.Add(o_adic);
            this.fechas_sac.Add(fec);
            this.libros.Add(lib);
            this.folios.Add(fol);
            this.asientos.Add(asi);
            this.pagos.Add(pag);
            this.deudas.Add(deu);
            this.guardar_datos();

            ApiReniec.LimpiarCache();
        }

        public void actualizar_completo(int idx, string d1, string nom1, string ape1, string d2, string nom2, string ape2, string gen, string tipo, string est, string t_pad, string est_pad, string chks, string o_auto, string o_adic, string fec, string lib, string fol, string asi, string pag, string deu)
        {
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
            this.origenes.RemoveAt(idx);
            this.dnis_prin.RemoveAt(idx);
            this.nombres.RemoveAt(idx);
            this.apellidos.RemoveAt(idx);
            this.dnis_sec.RemoveAt(idx);
            this.nombres_sec.RemoveAt(idx);
            this.apellidos_sec.RemoveAt(idx);
            this.generos.RemoveAt(idx);
            this.tipos.RemoveAt(idx);
            this.estados.RemoveAt(idx);
            this.tiene_padrinos.RemoveAt(idx);
            this.estado_padrinos.RemoveAt(idx);
            this.checks_mem.RemoveAt(idx);
            this.obs_auto.RemoveAt(idx);
            this.obs_adic.RemoveAt(idx);
            this.fechas_sac.RemoveAt(idx);
            this.libros.RemoveAt(idx);
            this.folios.RemoveAt(idx);
            this.asientos.RemoveAt(idx);
            this.pagos.RemoveAt(idx);
            this.deudas.RemoveAt(idx);
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

        public List<int> buscar_indices_por_dni(string dni, bool soloTramites = true)
        {
            List<int> resultados = new List<int>();
            int length = this.dnis_prin.Count;
            for (int i = 0; i < length; i++)
            {
                if (soloTramites && this.origenes[i] == "ANTIGUO")
                {
                    continue;
                }
                if (this.dnis_prin[i] == dni && dni != "SIN DNI" && dni != "---")
                {
                    resultados.Add(i);
                }
                else if (this.dnis_sec[i] == dni && dni != "" && dni != "---" && dni != "SIN DNI")
                {
                    resultados.Add(i);
                }
            }
            return resultados;
        }

        public List<int> buscar_indices_por_apellidos(string apellidoBuscado, bool soloTramites = true)
        {
            List<int> resultados = new List<int>();
            string busqNorm = NormalizarSinTildes(apellidoBuscado.Trim());
            if (busqNorm == "")
            {
                return resultados;
            }

            int length = this.apellidos.Count;
            for (int i = 0; i < length; i++)
            {
                if (soloTramites && this.origenes[i] == "ANTIGUO")
                {
                    continue;
                }
                string ape1Norm = NormalizarSinTildes(this.apellidos[i]);
                string ape2Norm = NormalizarSinTildes(this.apellidos_sec[i]);

                if (ape1Norm.Contains(busqNorm) || (ape2Norm != "---" && ape2Norm.Contains(busqNorm)))
                {
                    resultados.Add(i);
                }
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
            int length = this.apellidos.Count;
            for (int i = 0; i < length; i++)
            {
                for (int j = 0; j < length - i - 1; j++)
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
            int length = this.apellidos.Count;
            for (int i = 0; i < length; i++)
            {
                int min_pos = i;
                for (int j = i + 1; j < length; j++)
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
        private static Dictionary<string, string> cacheNombres = new Dictionary<string, string>();
        private static Dictionary<string, string> cacheApellidos = new Dictionary<string, string>();

        public static void LimpiarCache()
        {
            cacheNombres.Clear();
            cacheApellidos.Clear();
        }

        private static string DecodificarTextoJson(string raw)
        {
            if (string.IsNullOrEmpty(raw))
            {
                return "";
            }
            try
            {
                return BaseDatos.A_MayusculasConTildes(Regex.Unescape(raw));
            }
            catch
            {
                return BaseDatos.A_MayusculasConTildes(raw);
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

            if (cacheNombres.ContainsKey(dni))
            {
                nombres = cacheNombres[dni];
                apellidos = cacheApellidos[dni];
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

                                    cacheNombres[dni] = nombres;
                                    cacheApellidos[dni] = apellidos;
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