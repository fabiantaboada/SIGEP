using System;
using System.Collections.Generic;
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
        public List<string> dnis_prin = new List<string>();
        public List<string> dnis_sec = new List<string>();
        public List<string> nombres = new List<string>();
        public List<string> nombres_sec = new List<string>();
        public List<string> correos = new List<string>();
        public List<string> generos = new List<string>();
        public List<string> tipos = new List<string>();
        public List<string> estados = new List<string>();
        public List<string> obs_auto = new List<string>();
        public List<string> obs_adic = new List<string>();
        public List<string> checks_mem = new List<string>();
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

        public static string EscaparCsv(string valor)
        {
            if (valor == null)
            {
                return "";
            }
            bool necesitaComillas = false;
            if (valor.Contains(","))
            {
                necesitaComillas = true;
            }
            if (valor.Contains("\""))
            {
                necesitaComillas = true;
            }
            if (valor.Contains("\n"))
            {
                necesitaComillas = true;
            }
            if (valor.Contains("\r"))
            {
                necesitaComillas = true;
            }

            if (necesitaComillas)
            {
                string limpio = valor.Replace("\"", "\"\"");
                return "\"" + limpio + "\"";
            }
            return valor;
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
                        filas.Add(filaActual.ToArray());
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
                filas.Add(filaActual.ToArray());
            }

            return filas;
        }

        public void crear_cabeceras()
        {
            string cabecera = "DNI_PRIN,DNI_SEC,NOMBRES,NOMBRES_SEC,CORREOS,GENERO,TIPO,ESTADO,OBS_AUTO,OBS_ADIC,CHECKS,FECHA_SAC,LIBRO,FOLIO,ASIENTO,PAGO,DEUDA\r\n";
            File.WriteAllText(this.archivo, cabecera, Encoding.UTF8);
        }

        public void limpiar_arreglos()
        {
            this.dnis_prin.Clear();
            this.dnis_sec.Clear();
            this.nombres.Clear();
            this.nombres_sec.Clear();
            this.correos.Clear();
            this.generos.Clear();
            this.tipos.Clear();
            this.estados.Clear();
            this.obs_auto.Clear();
            this.obs_adic.Clear();
            this.checks_mem.Clear();
            this.fechas_sac.Clear();
            this.libros.Clear();
            this.folios.Clear();
            this.asientos.Clear();
            this.pagos.Clear();
            this.deudas.Clear();
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
                for (int i = 1; i < filas.Count; i++)
                {
                    string[] fila = filas[i];
                    if (fila.Length == 17)
                    {
                        this.dnis_prin.Add(fila[0]);
                        this.dnis_sec.Add(fila[1]);
                        this.nombres.Add(fila[2]);
                        this.nombres_sec.Add(fila[3]);
                        this.correos.Add(fila[4]);
                        this.generos.Add(fila[5]);
                        this.tipos.Add(fila[6]);
                        this.estados.Add(fila[7]);
                        this.obs_auto.Add(fila[8]);
                        this.obs_adic.Add(fila[9]);
                        this.checks_mem.Add(fila[10]);
                        this.fechas_sac.Add(fila[11]);
                        this.libros.Add(fila[12]);
                        this.folios.Add(fila[13]);
                        this.asientos.Add(fila[14]);
                        this.pagos.Add(fila[15]);
                        this.deudas.Add(fila[16]);
                    }
                }
            }
        }

        public void guardar_datos()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("DNI_PRIN,DNI_SEC,NOMBRES,NOMBRES_SEC,CORREOS,GENERO,TIPO,ESTADO,OBS_AUTO,OBS_ADIC,CHECKS,FECHA_SAC,LIBRO,FOLIO,ASIENTO,PAGO,DEUDA");
            int length = this.dnis_prin.Count;
            for (int i = 0; i < length; i++)
            {
                string linea = string.Join(",", new string[] {
                    EscaparCsv(this.dnis_prin[i]),
                    EscaparCsv(this.dnis_sec[i]),
                    EscaparCsv(this.nombres[i]),
                    EscaparCsv(this.nombres_sec[i]),
                    EscaparCsv(this.correos[i]),
                    EscaparCsv(this.generos[i]),
                    EscaparCsv(this.tipos[i]),
                    EscaparCsv(this.estados[i]),
                    EscaparCsv(this.obs_auto[i]),
                    EscaparCsv(this.obs_adic[i]),
                    EscaparCsv(this.checks_mem[i]),
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

        public void registrar(string d1, string d2, string nom, string nom2, string cor, string gen, string tipo, string est, string o_auto, string o_adic, string chks, string fec, string lib, string fol, string asi, string pag, string deu)
        {
            this.dnis_prin.Add(d1);
            this.dnis_sec.Add(d2);
            this.nombres.Add(nom);
            this.nombres_sec.Add(nom2);
            this.correos.Add(cor);
            this.generos.Add(gen);
            this.tipos.Add(tipo);
            this.estados.Add(est);
            this.obs_auto.Add(o_auto);
            this.obs_adic.Add(o_adic);
            this.checks_mem.Add(chks);
            this.fechas_sac.Add(fec);
            this.libros.Add(lib);
            this.folios.Add(fol);
            this.asientos.Add(asi);
            this.pagos.Add(pag);
            this.deudas.Add(deu);
            this.guardar_datos();

            ApiReniec.LimpiarCache();
        }

        public void actualizar_completo(int idx, string d1, string d2, string nom, string nom2, string cor, string gen, string est, string o_auto, string o_adic, string chks, string fec, string lib, string fol, string asi, string pag, string deu)
        {
            this.dnis_prin[idx] = d1;
            this.dnis_sec[idx] = d2;
            this.nombres[idx] = nom;
            this.nombres_sec[idx] = nom2;
            this.correos[idx] = cor;
            this.generos[idx] = gen;
            this.estados[idx] = est;
            this.obs_auto[idx] = o_auto;
            this.obs_adic[idx] = o_adic;
            this.checks_mem[idx] = chks;
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
            this.dnis_prin.RemoveAt(idx);
            this.dnis_sec.RemoveAt(idx);
            this.nombres.RemoveAt(idx);
            this.nombres_sec.RemoveAt(idx);
            this.correos.RemoveAt(idx);
            this.generos.RemoveAt(idx);
            this.tipos.RemoveAt(idx);
            this.estados.RemoveAt(idx);
            this.obs_auto.RemoveAt(idx);
            this.obs_adic.RemoveAt(idx);
            this.checks_mem.RemoveAt(idx);
            this.fechas_sac.RemoveAt(idx);
            this.libros.RemoveAt(idx);
            this.folios.RemoveAt(idx);
            this.asientos.RemoveAt(idx);
            this.pagos.RemoveAt(idx);
            this.deudas.RemoveAt(idx);
            this.guardar_datos();
        }

        public int buscar_por_dni(string dni)
        {
            int length = this.dnis_prin.Count;
            for (int i = 0; i < length; i++)
            {
                if (this.dnis_prin[i] == dni)
                {
                    return i;
                }
                else if (this.dnis_sec[i] == dni && dni != "" && dni != "---")
                {
                    return i;
                }
            }
            return -1;
        }

        private void _swap(int i, int j)
        {
            string temp_d1 = this.dnis_prin[i];
            this.dnis_prin[i] = this.dnis_prin[j];
            this.dnis_prin[j] = temp_d1;

            string temp_d2 = this.dnis_sec[i];
            this.dnis_sec[i] = this.dnis_sec[j];
            this.dnis_sec[j] = temp_d2;

            string temp_nom = this.nombres[i];
            this.nombres[i] = this.nombres[j];
            this.nombres[j] = temp_nom;

            string temp_nom2 = this.nombres_sec[i];
            this.nombres_sec[i] = this.nombres_sec[j];
            this.nombres_sec[j] = temp_nom2;

            string temp_cor = this.correos[i];
            this.correos[i] = this.correos[j];
            this.correos[j] = temp_cor;

            string temp_gen = this.generos[i];
            this.generos[i] = this.generos[j];
            this.generos[j] = temp_gen;

            string temp_tip = this.tipos[i];
            this.tipos[i] = this.tipos[j];
            this.tipos[j] = temp_tip;

            string temp_est = this.estados[i];
            this.estados[i] = this.estados[j];
            this.estados[j] = temp_est;

            string temp_oba = this.obs_auto[i];
            this.obs_auto[i] = this.obs_auto[j];
            this.obs_auto[j] = temp_oba;

            string temp_obd = this.obs_adic[i];
            this.obs_adic[i] = this.obs_adic[j];
            this.obs_adic[j] = temp_obd;

            string temp_chk = this.checks_mem[i];
            this.checks_mem[i] = this.checks_mem[j];
            this.checks_mem[j] = temp_chk;

            string temp_fec = this.fechas_sac[i];
            this.fechas_sac[i] = this.fechas_sac[j];
            this.fechas_sac[j] = temp_fec;

            string temp_lib = this.libros[i];
            this.libros[i] = this.libros[j];
            this.libros[j] = temp_lib;

            string temp_fol = this.folios[i];
            this.folios[i] = this.folios[j];
            this.folios[j] = temp_fol;

            string temp_asi = this.asientos[i];
            this.asientos[i] = this.asientos[j];
            this.asientos[j] = temp_asi;

            string temp_pag = this.pagos[i];
            this.pagos[i] = this.pagos[j];
            this.pagos[j] = temp_pag;

            string temp_deu = this.deudas[i];
            this.deudas[i] = this.deudas[j];
            this.deudas[j] = temp_deu;
        }

        public void ordenar_bubble_asc()
        {
            int length = this.nombres.Count;
            for (int i = 0; i < length; i++)
            {
                for (int j = 0; j < length - i - 1; j++)
                {
                    if (string.Compare(this.nombres[j].ToLower(), this.nombres[j + 1].ToLower(), StringComparison.Ordinal) > 0)
                    {
                        this._swap(j, j + 1);
                    }
                }
            }
            this.guardar_datos();
        }

        public void ordenar_selection_desc()
        {
            int length = this.nombres.Count;
            for (int i = 0; i < length; i++)
            {
                int min_pos = i;
                for (int j = i + 1; j < length; j++)
                {
                    if (string.Compare(this.nombres[j].ToLower(), this.nombres[min_pos].ToLower(), StringComparison.Ordinal) > 0)
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

    // ================= API RENIEC CON CACHÉ TEMPORAL AUTOLIMPIABLE =================
    public static class ApiReniec
    {
        private static Dictionary<string, string> cacheNombres = new Dictionary<string, string>();
        private static Dictionary<string, string> cacheApellidos = new Dictionary<string, string>();

        public static void LimpiarCache()
        {
            cacheNombres.Clear();
            cacheApellidos.Clear();
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
                                    nombres = mNom.Groups[1].Value.Trim();
                                    string pat = mPat.Success ? mPat.Groups[1].Value.Trim() : "";
                                    string mat = mMat.Success ? mMat.Groups[1].Value.Trim() : "";
                                    apellidos = (pat + " " + mat).Trim();

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

        public static string consultar_dni(string dni)
        {
            string nom;
            string ape;
            if (consultar_dni_separado(dni, out nom, out ape))
            {
                return (nom + " " + ape).Trim();
            }
            return "";
        }
    }
}