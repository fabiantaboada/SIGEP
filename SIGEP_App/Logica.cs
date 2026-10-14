using System;
using System.Collections.Generic;
using System.IO;
using iTextSharp.text;
using iTextSharp.text.pdf;

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

        public bool generar_reporte_general_pdf(string ruta, List<ItemReporte> filtrados)
        {
            try
            {
                Document pdf = new Document(PageSize.A4, 40, 40, 40, 40);
                PdfWriter.GetInstance(pdf, new FileStream(ruta, FileMode.Create));
                pdf.Open();

                Font fTitulo = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 16);
                Font fSub = FontFactory.GetFont(FontFactory.HELVETICA, 10);
                Font fCab = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10);
                Font fCelda = FontFactory.GetFont(FontFactory.HELVETICA, 9);

                Paragraph pTit = new Paragraph("REPORTE GENERAL DE EXPEDIENTES", fTitulo);
                pTit.Alignment = Element.ALIGN_CENTER;
                pdf.Add(pTit);

                Paragraph pSub = new Paragraph("Parroquia San José de Bellavista - SIGEP\n\n", fSub);
                pSub.Alignment = Element.ALIGN_CENTER;
                pdf.Add(pSub);

                PdfPTable tabla = new PdfPTable(5);
                tabla.WidthPercentage = 100;
                tabla.SetWidths(new float[] { 35f, 22f, 13f, 15f, 15f });

                string[] cabeceras = { "Titular(es)", "Sacramento", "DNI", "Estado", "Deuda" };
                for (int i = 0; i < cabeceras.Length; i++)
                {
                    PdfPCell c = new PdfPCell(new Phrase(cabeceras[i], fCab));
                    c.HorizontalAlignment = Element.ALIGN_CENTER;
                    c.BackgroundColor = new BaseColor(241, 245, 249);
                    c.Padding = 6f;
                    tabla.AddCell(c);
                }

                for (int i = 0; i < filtrados.Count; i++)
                {
                    ItemReporte f = filtrados[i];
                    tabla.AddCell(new PdfPCell(new Phrase(f.nom, fCelda)) { Padding = 5f });
                    tabla.AddCell(new PdfPCell(new Phrase(f.tipo, fCelda)) { Padding = 5f });
                    tabla.AddCell(new PdfPCell(new Phrase(f.dni, fCelda)) { Padding = 5f });
                    tabla.AddCell(new PdfPCell(new Phrase(f.est, fCelda)) { Padding = 5f });

                    PdfPCell cDeuda = new PdfPCell(new Phrase(f.deuda, fCelda));
                    cDeuda.HorizontalAlignment = Element.ALIGN_CENTER;
                    cDeuda.Padding = 5f;
                    tabla.AddCell(cDeuda);
                }

                pdf.Add(tabla);
                pdf.Close();
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}