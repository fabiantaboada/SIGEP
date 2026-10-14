using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace SIGEP_App
{
    public class ItemReporte
    {
        public string nom;
        public string tipo;
        public string dni;
        public string est;
        public string deuda;
    }

    public class ControladorLogica
    {
        public bool validar_credenciales(string usuario, string password)
        {
            if (usuario == "admin")
            {
                if (password == "admin")
                {
                    return true;
                }
            }
            if (usuario == "secre")
            {
                if (password == "1234")
                {
                    return true;
                }
            }
            return false;
        }

        private string EscaparTextoPdf(string texto)
        {
            if (string.IsNullOrEmpty(texto))
            {
                return "";
            }
            StringBuilder sb = new StringBuilder();
            byte[] bytes = Encoding.GetEncoding(1252).GetBytes(texto);
            for (int i = 0; i < bytes.Length; i++)
            {
                byte b = bytes[i];
                if (b == '(' || b == ')' || b == '\\')
                {
                    sb.Append('\\').Append((char)b);
                }
                else if (b < 32 || b > 126)
                {
                    sb.Append('\\').Append(Convert.ToString(b, 8).PadLeft(3, '0'));
                }
                else
                {
                    sb.Append((char)b);
                }
            }
            return sb.ToString();
        }

        private string RecortarTexto(string texto, int maxCar)
        {
            if (string.IsNullOrEmpty(texto))
            {
                return "";
            }
            string limpio = texto.Replace("\r\n", " ").Replace("\n", " ").Trim();
            if (limpio.Length > maxCar)
            {
                return limpio.Substring(0, maxCar - 2) + "..";
            }
            return limpio;
        }

        // Generador nativo de PDF 1.4
        public bool generar_reporte_general_pdf(string ruta, List<ItemReporte> filtrados)
        {
            try
            {
                int filasPorPagina = 26;
                int totalPaginas = (int)Math.Ceiling(filtrados.Count / (double)filasPorPagina);
                if (totalPaginas == 0)
                {
                    totalPaginas = 1;
                }

                List<string> paginasContenido = new List<string>();

                // Coordenadas de columnas en hoja A4 (595 x 842 pt)
                int xTabla = 40;
                int[] anchos = { 195, 105, 65, 80, 70 };
                int anchoTotal = 515;
                string[] cabeceras = { "Titular(es)", "Sacramento", "DNI", "Estado", "Deuda" };

                for (int p = 0; p < totalPaginas; p++)
                {
                    StringBuilder Flujo = new StringBuilder();

                    // Título principal centrado
                    Flujo.AppendLine("BT /F2 15 Tf 145 785 Td (REPORTE GENERAL DE EXPEDIENTES) Tj ET");
                    Flujo.AppendLine("BT /F1 9 Tf 40 765 Td (Parroquia San Jose de Bellavista - SIGEP) Tj ET");

                    int yTop = 745;
                    int altoFila = 22;

                    // Fondo gris de cabecera de tabla
                    Flujo.AppendLine("0.92 0.94 0.96 rg");
                    Flujo.AppendLine(string.Format("{0} {1} {2} {3} re f", xTabla, yTop - altoFila, anchoTotal, altoFila));
                    Flujo.AppendLine("0 0 0 rg");

                    // Borde de cabecera y textos
                    Flujo.AppendLine("0.7 w");
                    int xActual = xTabla;
                    for (int c = 0; c < cabeceras.Length; c++)
                    {
                        Flujo.AppendLine(string.Format("{0} {1} {2} {3} re S", xActual, yTop - altoFila, anchos[c], altoFila));
                        Flujo.AppendLine(string.Format("BT /F2 9.5 Tf {0} {1} Td ({2}) Tj ET", xActual + 5, yTop - 15, EscaparTextoPdf(cabeceras[c])));
                        xActual += anchos[c];
                    }

                    int yFila = yTop - altoFila;
                    int inicio = p * filasPorPagina;
                    int fin = Math.Min(inicio + filasPorPagina, filtrados.Count);

                    for (int i = inicio; i < fin; i++)
                    {
                        ItemReporte item = filtrados[i];
                        string[] celdas = {
                            RecortarTexto(item.nom, 36),
                            RecortarTexto(item.tipo, 18),
                            RecortarTexto(item.dni, 10),
                            RecortarTexto(item.est, 14),
                            RecortarTexto(item.deuda, 13)
                        };

                        xActual = xTabla;
                        for (int c = 0; c < 5; c++)
                        {
                            Flujo.AppendLine(string.Format("{0} {1} {2} {3} re S", xActual, yFila - altoFila, anchos[c], altoFila));
                            Flujo.AppendLine(string.Format("BT /F1 8.5 Tf {0} {1} Td ({2}) Tj ET", xActual + 5, yFila - 14, EscaparTextoPdf(celdas[c])));
                            xActual += anchos[c];
                        }
                        yFila -= altoFila;
                    }

                    // Pie de página
                    Flujo.AppendLine(string.Format("BT /F1 8.5 Tf 260 35 Td (Pagina {0} de {1}) Tj ET", p + 1, totalPaginas));
                    paginasContenido.Add(Flujo.ToString());
                }

                // Construcción estructural del documento PDF
                using (FileStream fs = new FileStream(ruta, FileMode.Create, FileAccess.Write))
                using (StreamWriter sw = new StreamWriter(fs, Encoding.ASCII))
                {
                    List<long> offsets = new List<long>();
                    Action<string> EscribirObj = (contenidoObj) =>
                    {
                        sw.Flush();
                        offsets.Add(fs.Position);
                        int idObj = offsets.Count;
                        sw.WriteLine(idObj + " 0 obj");
                        sw.WriteLine(contenidoObj);
                        sw.WriteLine("endobj");
                    };

                    sw.WriteLine("%PDF-1.4");

                    // 1: Catalog, 2: Pages, 3: Font Regular, 4: Font Bold
                    EscribirObj("<< /Type /Catalog /Pages 2 0 R >>");

                    List<string> refsPaginas = new List<string>();
                    for (int p = 0; p < totalPaginas; p++)
                    {
                        int idPageObj = 5 + (p * 2);
                        refsPaginas.Add(idPageObj + " 0 R");
                    }
                    EscribirObj("<< /Type /Pages /Kids [ " + string.Join(" ", refsPaginas) + " ] /Count " + totalPaginas + " >>");
                    EscribirObj("<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica /Encoding /WinAnsiEncoding >>");
                    EscribirObj("<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica-Bold /Encoding /WinAnsiEncoding >>");

                    for (int p = 0; p < totalPaginas; p++)
                    {
                        int idContentObj = 6 + (p * 2);
                        string flujoPag = paginasContenido[p];
                        int longitudBytes = Encoding.ASCII.GetByteCount(flujoPag);

                        EscribirObj("<< /Type /Page /Parent 2 0 R /MediaBox [0 0 595 842] /Resources << /Font << /F1 3 0 R /F2 4 0 R >> >> /Contents " + idContentObj + " 0 R >>");
                        EscribirObj("<< /Length " + longitudBytes + " >>\nstream\n" + flujoPag + "endstream");
                    }

                    sw.Flush();
                    long posXref = fs.Position;
                    sw.WriteLine("xref");
                    sw.WriteLine("0 " + (offsets.Count + 1));
                    sw.WriteLine("0000000000 65535 f ");
                    for (int i = 0; i < offsets.Count; i++)
                    {
                        sw.WriteLine(offsets[i].ToString("D10") + " 00000 n ");
                    }
                    sw.WriteLine("trailer");
                    sw.WriteLine("<< /Size " + (offsets.Count + 1) + " /Root 1 0 R >>");
                    sw.WriteLine("startxref");
                    sw.WriteLine(posXref.ToString());
                    sw.WriteLine("%%EOF");
                }
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}