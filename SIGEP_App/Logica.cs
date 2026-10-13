using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Mail;
using System.Text.RegularExpressions;
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

        public bool validar_correo_regex(string correo)
        {
            string patron = @"^[\w\.-]+@[\w\.-]+\.\w+$";
            if (Regex.IsMatch(correo, patron))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public string obtener_fecha_actual()
        {
            string[] meses = { "Enero", "Febrero", "Marzo", "Abril", "Mayo", "Junio", "Julio", "Agosto", "Septiembre", "Octubre", "Noviembre", "Diciembre" };
            DateTime now = DateTime.Now;
            int mes_index = now.Month - 1;
            return now.Day + " de " + meses[mes_index] + " de " + now.Year;
        }

        public string formatear_dia_mes(string fecha_str)
        {
            if (fecha_str == "--/--")
            {
                return "en una fecha por definir";
            }
            if (fecha_str == "")
            {
                return "en una fecha por definir";
            }

            try
            {
                string[] partes = fecha_str.Split('/');
                if (partes.Length >= 2)
                {
                    string[] meses = { "Enero", "Febrero", "Marzo", "Abril", "Mayo", "Junio", "Julio", "Agosto", "Septiembre", "Octubre", "Noviembre", "Diciembre" };
                    int dia = int.Parse(partes[0]);
                    int mes_num = int.Parse(partes[1]);
                    int mes_index = mes_num - 1;
                    return "el " + dia + " de " + meses[mes_index] + " del presente año";
                }
            }
            catch
            {
            }
            return "en una fecha por definir";
        }

        public bool generar_constancia_inscripcion(string ruta, string titular, string apoderado, string dni_titular, string genero, string sacramento, Configuracion config)
        {
            try
            {
                Document pdf = new Document(PageSize.A4, 70, 70, 70, 70);
                PdfWriter.GetInstance(pdf, new FileStream(ruta, FileMode.Create));
                pdf.Open();

                string ruta_img = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "assets", "sanjose.png");
                if (File.Exists(ruta_img))
                {
                    Image img = Image.GetInstance(ruta_img);
                    img.ScaleToFit(60f, 60f);
                    img.SetAbsolutePosition(70f, 735f);
                    pdf.Add(img);
                }

                Font fBold15 = FontFactory.GetFont(FontFactory.TIMES_BOLD, 15);
                Font fNorm14 = FontFactory.GetFont(FontFactory.TIMES_ROMAN, 14);
                Font fBold14 = FontFactory.GetFont(FontFactory.TIMES_BOLD, 14);
                Font fItal9 = FontFactory.GetFont(FontFactory.TIMES_ITALIC, 9);
                Font fNorm11 = FontFactory.GetFont(FontFactory.TIMES_ROMAN, 11);

                Paragraph cab1 = new Paragraph("Parroquia \"San José de Bellavista\"", fBold15);
                cab1.IndentationLeft = 75f;
                pdf.Add(cab1);

                Paragraph cab2 = new Paragraph("Diócesis del Callao", fNorm14);
                cab2.IndentationLeft = 75f;
                pdf.Add(cab2);

                pdf.Add(new Paragraph("\n\n\n"));

                string etiqueta = "";
                if (sacramento == "Matrimonio Regular")
                {
                    etiqueta = "Los Novios:";
                }
                else if (sacramento == "Confirmación")
                {
                    if (genero == "Femenino")
                    {
                        etiqueta = "La Joven:";
                    }
                    else
                    {
                        etiqueta = "El Joven:";
                    }
                }
                else
                {
                    if (genero == "Femenino")
                    {
                        etiqueta = "La Niña:";
                    }
                    else
                    {
                        etiqueta = "El Niño:";
                    }
                }

                Paragraph pEtiq = new Paragraph(etiqueta, fBold14);
                pEtiq.Alignment = Element.ALIGN_CENTER;
                pdf.Add(pEtiq);

                if (sacramento == "Matrimonio Regular")
                {
                    Paragraph pT1 = new Paragraph(titular.ToUpper(), fBold14);
                    pT1.Alignment = Element.ALIGN_CENTER;
                    pdf.Add(pT1);

                    Paragraph pT2 = new Paragraph(apoderado.ToUpper(), fBold14);
                    pT2.Alignment = Element.ALIGN_CENTER;
                    pdf.Add(pT2);
                }
                else
                {
                    Paragraph pT1 = new Paragraph(titular.ToUpper(), fBold14);
                    pT1.Alignment = Element.ALIGN_CENTER;
                    pdf.Add(pT1);
                }

                Paragraph pDni = new Paragraph("DNI " + dni_titular, fNorm14);
                pDni.Alignment = Element.ALIGN_CENTER;
                pdf.Add(pDni);

                pdf.Add(new Paragraph("\n\n"));

                string inscrito = "";
                if (genero == "Femenino")
                {
                    inscrito = "inscrita";
                }
                else
                {
                    inscrito = "inscrito";
                }

                if (sacramento == "Matrimonio Regular")
                {
                    inscrito = "inscritos";
                }

                string articulo = "";
                if (sacramento == "Primera Comunión")
                {
                    articulo = "la";
                }
                else if (sacramento == "Confirmación")
                {
                    articulo = "la";
                }
                else
                {
                    articulo = "el";
                }

                string cuerpo = "";
                if (sacramento == "Primera Comunión")
                {
                    string fecha_formato = this.formatear_dia_mes(config.fecha_comunion);
                    cuerpo = "Está " + inscrito + " en la Catequesis de preparación para la " + sacramento + ", el cual comienza " + fecha_formato + ".";
                }
                else if (sacramento == "Confirmación")
                {
                    string fecha_formato = this.formatear_dia_mes(config.fecha_confirmacion);
                    cuerpo = "Está " + inscrito + " en la Catequesis de preparación para la " + sacramento + ", el cual comienza " + fecha_formato + ".";
                }
                else
                {
                    cuerpo = "Se hace constar que se ha procesado exitosamente la solicitud de inscripción para " + articulo + " " + sacramento + ". Queda a la espera de la asignación de la fecha oficial de celebración.";
                }

                Paragraph pCuerpo = new Paragraph(cuerpo, fNorm14);
                pCuerpo.Alignment = Element.ALIGN_JUSTIFIED;
                pdf.Add(pCuerpo);

                pdf.Add(new Paragraph("\n\n\n"));

                Paragraph pFec = new Paragraph("Bellavista, " + this.obtener_fecha_actual(), fNorm14);
                pFec.Alignment = Element.ALIGN_LEFT;
                pdf.Add(pFec);

                pdf.Add(new Paragraph("\n\n\n\n\n\n\n\n"));

                Paragraph pie1 = new Paragraph("Documento autogenerado por SIGEP", fItal9);
                pie1.Alignment = Element.ALIGN_CENTER;
                pdf.Add(pie1);

                Paragraph pie2 = new Paragraph("Jirón Bolognesi No. 440 - Bellavista, Callao - Celular: 933352152", fNorm11);
                pie2.Alignment = Element.ALIGN_CENTER;
                pdf.Add(pie2);

                Paragraph pie3 = new Paragraph("Email: psanjosedebellavista@gmail.com", fNorm11);
                pie3.Alignment = Element.ALIGN_CENTER;
                pdf.Add(pie3);

                pdf.Close();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public bool generar_partida_generica(string ruta, string titular, string apoderado, string sacramento, string libro, string folio, string asiento)
        {
            try
            {
                Document pdf = new Document(PageSize.A4, 70, 70, 70, 70);
                PdfWriter.GetInstance(pdf, new FileStream(ruta, FileMode.Create));
                pdf.Open();

                Font fBold18 = FontFactory.GetFont(FontFactory.TIMES_BOLD, 18);
                Font fItal14 = FontFactory.GetFont(FontFactory.TIMES_ITALIC, 14);
                Font fBold16 = FontFactory.GetFont(FontFactory.TIMES_BOLD, 16);
                Font fNorm13 = FontFactory.GetFont(FontFactory.TIMES_ROMAN, 13);
                Font fBold12 = FontFactory.GetFont(FontFactory.TIMES_BOLD, 12);
                Font fItal9 = FontFactory.GetFont(FontFactory.TIMES_ITALIC, 9);
                Font fNorm11 = FontFactory.GetFont(FontFactory.TIMES_ROMAN, 11);

                Paragraph p1 = new Paragraph("PARROQUIA SAN JOSÉ DE BELLAVISTA", fBold18);
                p1.Alignment = Element.ALIGN_CENTER;
                pdf.Add(p1);

                Paragraph p2 = new Paragraph("DIÓCESIS DEL CALLAO", fItal14);
                p2.Alignment = Element.ALIGN_CENTER;
                pdf.Add(p2);

                pdf.Add(new Paragraph("\n\n"));

                Paragraph p3 = new Paragraph("CONSTANCIA DE " + sacramento.ToUpper(), fBold16);
                p3.Alignment = Element.ALIGN_CENTER;
                pdf.Add(p3);

                pdf.Add(new Paragraph("\n\n"));

                string texto = "";
                if (sacramento == "Matrimonio Regular")
                {
                    texto = "Certificamos de manera oficial que los esposos " + titular.ToUpper() + ", " + apoderado.ToUpper() + " han cumplido satisfactoriamente con los requisitos canónicos correspondientes.";
                }
                else
                {
                    texto = "Certificamos de manera oficial que " + titular.ToUpper() + " ha cumplido satisfactoriamente con los requisitos canónicos correspondientes.";
                }

                Paragraph pTexto = new Paragraph(texto, fNorm13);
                pTexto.Alignment = Element.ALIGN_JUSTIFIED;
                pdf.Add(pTexto);

                pdf.Add(new Paragraph("\n\n"));

                Paragraph pReg = new Paragraph("REGISTRO: Libro " + libro + " | Folio " + folio + " | Asiento " + asiento, fBold12);
                pReg.Alignment = Element.ALIGN_LEFT;
                pdf.Add(pReg);

                pdf.Add(new Paragraph("\n\n\n\n"));

                Paragraph pFirma1 = new Paragraph("_______________", fBold12);
                pFirma1.Alignment = Element.ALIGN_CENTER;
                pdf.Add(pFirma1);

                Paragraph pFirma2 = new Paragraph("Firma y Sello Oficial", fBold12);
                pFirma2.Alignment = Element.ALIGN_CENTER;
                pdf.Add(pFirma2);

                pdf.Add(new Paragraph("\n\n\n\n\n\n"));

                Paragraph pie1 = new Paragraph("Documento autogenerado por SIGEP", fItal9);
                pie1.Alignment = Element.ALIGN_CENTER;
                pdf.Add(pie1);

                Paragraph pie2 = new Paragraph("Jirón Bolognesi No. 440 - Bellavista, Callao - Celular: 933352152", fNorm11);
                pie2.Alignment = Element.ALIGN_CENTER;
                pdf.Add(pie2);

                Paragraph pie3 = new Paragraph("Email: psanjosedebellavista@gmail.com", fNorm11);
                pie3.Alignment = Element.ALIGN_CENTER;
                pdf.Add(pie3);

                pdf.Close();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public bool generar_reporte_general_pdf(string ruta, List<ItemReporte> filtrados)
        {
            try
            {
                Document pdf = new Document(PageSize.A4, 40, 40, 40, 40);
                PdfWriter.GetInstance(pdf, new FileStream(ruta, FileMode.Create));
                pdf.Open();

                Font fTitulo = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 16);
                Font fCab = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10);
                Font fCelda = FontFactory.GetFont(FontFactory.HELVETICA, 9);

                Paragraph pTit = new Paragraph("REPORTE GENERAL DE EXPEDIENTES", fTitulo);
                pTit.Alignment = Element.ALIGN_CENTER;
                pdf.Add(pTit);
                pdf.Add(new Paragraph("\n"));

                PdfPTable tabla = new PdfPTable(5);
                tabla.WidthPercentage = 100;
                tabla.SetWidths(new float[] { 35f, 22f, 13f, 15f, 15f });

                string[] cabeceras = { "Titular", "Sacramento", "DNI", "Estado", "Deuda" };
                for (int i = 0; i < cabeceras.Length; i++)
                {
                    PdfPCell c = new PdfPCell(new Phrase(cabeceras[i], fCab));
                    c.HorizontalAlignment = Element.ALIGN_CENTER;
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

        public bool enviar_correo(string correos, string titular, string apoderado, string sacramento, string estado, string obs_auto, string obs_adic, string pago, string deuda, string ruta_adjunto = null)
        {
            try
            {
                string emisor = "pruebapsjb@gmail.com";
                string clave = "qzfklybrpxaiafjd";
                List<string> lista_correos = new List<string>();

                string[] correos_separados = correos.Split(',');
                for (int i = 0; i < correos_separados.Length; i++)
                {
                    string correo_limpio = correos_separados[i].Trim();
                    if (this.validar_correo_regex(correo_limpio))
                    {
                        lista_correos.Add(correo_limpio);
                    }
                }

                if (lista_correos.Count == 0)
                {
                    return false;
                }

                MailMessage msg = new MailMessage();
                msg.From = new MailAddress(emisor);
                for (int i = 0; i < lista_correos.Count; i++)
                {
                    msg.To.Add(lista_correos[i]);
                }
                msg.Subject = "Documentación y Estado de Trámite Parroquial - San José de Bellavista";

                string saludo = "";
                if (sacramento == "Matrimonio Regular")
                {
                    saludo = titular + ", " + apoderado;
                }
                else
                {
                    saludo = apoderado;
                }

                string cuerpo = "Estimado(a) " + saludo + ",\n\nEl trámite en nuestra parroquia ha sido registrado y/o actualizado.\n\nESTADO ACTUAL: " + estado + "\n";

                if (pago == "Falta Pagar")
                {
                    cuerpo += "\nRESUMEN DE REQUISITOS:\n" + obs_auto + "\n";
                    if (!string.IsNullOrEmpty(obs_adic))
                    {
                        cuerpo += "Notas Adicionales: " + obs_adic + "\n";
                    }
                }
                else if (obs_auto.Contains("Falta"))
                {
                    cuerpo += "\nRESUMEN DE REQUISITOS:\n" + obs_auto + "\n";
                    if (!string.IsNullOrEmpty(obs_adic))
                    {
                        cuerpo += "Notas Adicionales: " + obs_adic + "\n";
                    }
                }
                else if (!string.IsNullOrEmpty(obs_adic))
                {
                    cuerpo += "\nRESUMEN DE REQUISITOS:\n" + obs_auto + "\n";
                    cuerpo += "Notas Adicionales: " + obs_adic + "\n";
                }

                if (pago == "Falta Pagar")
                {
                    cuerpo += "\n[ AVISO IMPORTANTE ]: Presenta un saldo pendiente de S/ " + deuda + ". Acérquese a secretaría para regularizar y obtener su constancia oficial.\n";
                }
                else if (!string.IsNullOrEmpty(ruta_adjunto))
                {
                    cuerpo += "\nAdjuntamos a este correo su documento oficial en formato PDF.\n";
                }

                cuerpo += "\nAtentamente,\nSecretaría Parroquial - San José de Bellavista.";
                msg.Body = cuerpo;

                if (pago == "Pagado")
                {
                    if (!string.IsNullOrEmpty(ruta_adjunto))
                    {
                        if (File.Exists(ruta_adjunto))
                        {
                            msg.Attachments.Add(new Attachment(ruta_adjunto));
                        }
                    }
                }

                SmtpClient server = new SmtpClient("smtp.gmail.com", 587);
                server.EnableSsl = true;
                server.Credentials = new NetworkCredential(emisor, clave);
                server.Send(msg);
                msg.Dispose();
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}