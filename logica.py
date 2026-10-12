from fpdf import FPDF
import smtplib
from email.message import EmailMessage
import re
import datetime
import os

class ControladorLogica:
    def validar_credenciales(self, usuario, password):
        if usuario == "admin":
            if password == "admin":
                return True
        if usuario == "secre":
            if password == "1234":
                return True
        return False

    def validar_correo_regex(self, correo):
        patron = r'^[\w\.-]+@[\w\.-]+\.\w+$'
        if re.match(patron, correo) is not None:
            return True
        else:
            return False

    def obtener_fecha_actual(self):
        meses = ["Enero", "Febrero", "Marzo", "Abril", "Mayo", "Junio", "Julio", "Agosto", "Septiembre", "Octubre", "Noviembre", "Diciembre"]
        now = datetime.datetime.now()
        mes_index = now.month - 1
        return f"{now.day} de {meses[mes_index]} de {now.year}"

    def formatear_dia_mes(self, fecha_str):
        if fecha_str == "--/--":
            return "en una fecha por definir"
        if fecha_str == "":
            return "en una fecha por definir"
            
        try:
            partes = fecha_str.split("/")
            if len(partes) >= 2:
                meses = ["Enero", "Febrero", "Marzo", "Abril", "Mayo", "Junio", "Julio", "Agosto", "Septiembre", "Octubre", "Noviembre", "Diciembre"]
                mes_num = int(partes[1])
                mes_index = mes_num - 1
                return f"el {int(partes[0])} de {meses[mes_index]} del presente año"
        except:
            pass
        return "en una fecha por definir"

    def generar_constancia_inscripcion(self, ruta, titular, apoderado, dni_titular, genero, sacramento, config):
        try:
            pdf = FPDF()
            pdf.set_auto_page_break(auto=False)
            pdf.add_page()
            pdf.set_margins(25, 25, 25)
            pdf.set_text_color(0, 0, 0)
            
            ruta_img = os.path.join(os.path.dirname(__file__), 'assets', 'sanjose.png')
            if os.path.exists(ruta_img):
                pdf.image(ruta_img, x=25, y=20, w=22)
            
            pdf.set_xy(50, 25)
            pdf.set_font("Times", 'B', 15)
            pdf.cell(0, 6, 'Parroquia "San José de Bellavista"', ln=True, align='L')
            pdf.set_xy(50, 31)
            pdf.set_font("Times", '', 14)
            pdf.cell(0, 6, "Diócesis del Callao", ln=True, align='L')
            pdf.ln(35)
            
            etiqueta = ""
            if sacramento == "Matrimonio Regular":
                etiqueta = "Los Novios:"
            elif sacramento == "Confirmación":
                if genero == "Femenino":
                    etiqueta = "La Joven:"
                else:
                    etiqueta = "El Joven:"
            else:
                if genero == "Femenino":
                    etiqueta = "La Niña:"
                else:
                    etiqueta = "El Niño:"
                
            pdf.set_font("Times", 'B', 14)
            pdf.cell(0, 8, etiqueta, ln=True, align='C')
            
            if sacramento == "Matrimonio Regular":
                pdf.cell(0, 8, titular.upper(), ln=True, align='C')
                pdf.cell(0, 8, apoderado.upper(), ln=True, align='C')
            else:
                pdf.cell(0, 8, titular.upper(), ln=True, align='C')
            
            pdf.set_font("Times", '', 14)
            pdf.cell(0, 8, f"DNI {dni_titular}", ln=True, align='C')
            pdf.ln(15)
            
            inscrito = ""
            if genero == "Femenino":
                inscrito = "inscrita"
            else:
                inscrito = "inscrito"
                
            if sacramento == "Matrimonio Regular":
                inscrito = "inscritos"
                
            articulo = ""
            if sacramento == "Primera Comunión":
                articulo = "la"
            elif sacramento == "Confirmación":
                articulo = "la"
            else:
                articulo = "el"
            
            cuerpo = ""
            if sacramento == "Primera Comunión":
                fecha_formato = self.formatear_dia_mes(config.fecha_comunion)
                cuerpo = f"Está {inscrito} en la Catequesis de preparación para la {sacramento}, el cual comienza {fecha_formato}."
            elif sacramento == "Confirmación":
                fecha_formato = self.formatear_dia_mes(config.fecha_confirmacion)
                cuerpo = f"Está {inscrito} en la Catequesis de preparación para la {sacramento}, el cual comienza {fecha_formato}."
            else:
                cuerpo = f"Se hace constar que se ha procesado exitosamente la solicitud de inscripción para {articulo} {sacramento}. Queda a la espera de la asignación de la fecha oficial de celebración."

            pdf.set_font("Times", '', 14)
            pdf.multi_cell(0, 8, cuerpo, align='J')
            pdf.ln(20)
            
            fecha_str = f"Bellavista, {self.obtener_fecha_actual()}"
            pdf.cell(0, 8, fecha_str, ln=True, align='L')
            
            pdf.set_y(260)
            pdf.set_font("Times", 'I', 9)
            pdf.cell(0, 5, "Documento autogenerado por SIGEP", ln=True, align='C')
            pdf.set_font("Times", '', 11)
            pdf.cell(0, 5, "Jirón Bolognesi No. 440 - Bellavista, Callao - Celular: 933352152", ln=True, align='C')
            pdf.cell(0, 5, "Email: psanjosedebellavista@gmail.com", ln=True, align='C')
            pdf.output(ruta)
            return True
        except:
            return False

    def generar_partida_generica(self, ruta, titular, apoderado, sacramento, libro, folio, asiento):
        try:
            pdf = FPDF()
            pdf.add_page()
            pdf.set_text_color(0, 0, 0)
            pdf.set_font("Times", 'B', 18)
            pdf.cell(200, 10, txt="PARROQUIA SAN JOSÉ DE BELLAVISTA", ln=True, align='C')
            pdf.set_font("Times", 'I', 14)
            pdf.cell(200, 10, txt="DIÓCESIS DEL CALLAO", ln=True, align='C')
            pdf.ln(15)
            pdf.set_font("Times", 'B', 16)
            pdf.cell(200, 10, txt=f"CONSTANCIA DE {sacramento.upper()}", ln=True, align='C')
            pdf.ln(10)
            pdf.set_font("Times", '', 13)
            
            texto = ""
            if sacramento == "Matrimonio Regular":
                texto = f"Certificamos de manera oficial que los esposos {titular.upper()}, {apoderado.upper()} han cumplido satisfactoriamente con los requisitos canónicos correspondientes."
            else:
                texto = f"Certificamos de manera oficial que {titular.upper()} ha cumplido satisfactoriamente con los requisitos canónicos correspondientes."
            
            pdf.multi_cell(0, 8, texto)
            pdf.ln(15)
            pdf.set_font("Times", 'B', 12)
            pdf.cell(200, 10, txt=f"REGISTRO: Libro {libro} | Folio {folio} | Asiento {asiento}", ln=True, align='L')
            pdf.ln(30)
            pdf.cell(200, 10, txt="_______________", ln=True, align='C')
            pdf.cell(200, 10, txt="Firma y Sello Oficial", ln=True, align='C')
            pdf.set_y(260)
            pdf.set_font("Times", 'I', 9)
            pdf.cell(0, 5, "Documento autogenerado por SIGEP", ln=True, align='C')
            pdf.set_font("Times", '', 11)
            pdf.cell(0, 5, "Jirón Bolognesi No. 440 - Bellavista, Callao - Celular: 933352152", ln=True, align='C')
            pdf.cell(0, 5, "Email: psanjosedebellavista@gmail.com", ln=True, align='C')
            pdf.output(ruta)
            return True
        except:
            return False

    def generar_reporte_general_pdf(self, ruta, filtrados):
        try:
            pdf = FPDF()
            pdf.add_page()
            pdf.set_font("Arial", 'B', 16)
            pdf.cell(200, 10, txt="REPORTE GENERAL DE EXPEDIENTES", ln=True, align='C')
            pdf.ln(10)
            
            pdf.set_font("Arial", 'B', 10)
            pdf.cell(70, 10, "Titular", 1, 0, 'C')
            pdf.cell(45, 10, "Sacramento", 1, 0, 'C')
            pdf.cell(25, 10, "DNI", 1, 0, 'C')
            pdf.cell(30, 10, "Estado", 1, 0, 'C')
            pdf.cell(20, 10, "Deuda", 1, 1, 'C')
            
            pdf.set_font("Arial", '', 9)
            for f in filtrados:
                pdf.cell(70, 10, f['nom'][:30], 1)
                pdf.cell(45, 10, f['tipo'][:20], 1)
                pdf.cell(25, 10, f['dni'], 1)
                pdf.cell(30, 10, f['est'], 1)
                pdf.cell(20, 10, f['deuda'], 1, 1, 'C')
            pdf.output(ruta)
            return True
        except:
            return False

    def enviar_correo(self, correos, titular, apoderado, sacramento, estado, obs_auto, obs_adic, pago, deuda, ruta_adjunto=None):
        try:
            emisor = "pruebapsjb@gmail.com"
            clave = "qzfklybrpxaiafjd"
            lista_correos = []
            
            correos_separados = correos.split(',')
            for c in correos_separados:
                correo_limpio = c.strip()
                if self.validar_correo_regex(correo_limpio):
                    lista_correos.append(correo_limpio)
                    
            if len(lista_correos) == 0:
                return False
            
            msg = EmailMessage()
            msg['Subject'] = "Documentación y Estado de Trámite Parroquial - San José de Bellavista"
            msg['From'] = emisor
            msg['To'] = ", ".join(lista_correos)
            
            saludo = ""
            if sacramento == "Matrimonio Regular":
                saludo = f"{titular}, {apoderado}"
            else:
                saludo = apoderado 
                
            cuerpo = f"Estimado(a) {saludo},\n\nEl trámite en nuestra parroquia ha sido registrado y/o actualizado.\n\nESTADO ACTUAL: {estado}\n"
            
            if pago == "Falta Pagar":
                cuerpo += f"\nRESUMEN DE REQUISITOS:\n{obs_auto}\n"
                if obs_adic:
                    cuerpo += f"Notas Adicionales: {obs_adic}\n"
            elif "Falta" in obs_auto:
                cuerpo += f"\nRESUMEN DE REQUISITOS:\n{obs_auto}\n"
                if obs_adic:
                    cuerpo += f"Notas Adicionales: {obs_adic}\n"
            elif obs_adic:
                cuerpo += f"\nRESUMEN DE REQUISITOS:\n{obs_auto}\n"
                cuerpo += f"Notas Adicionales: {obs_adic}\n"
            
            if pago == "Falta Pagar": 
                cuerpo += f"\n[ AVISO IMPORTANTE ]: Presenta un saldo pendiente de S/ {deuda}. Acérquese a secretaría para regularizar y obtener su constancia oficial.\n"
            elif ruta_adjunto: 
                cuerpo += "\nAdjuntamos a este correo su documento oficial en formato PDF.\n"
                
            cuerpo += "\nAtentamente,\nSecretaría Parroquial - San José de Bellavista."
            msg.set_content(cuerpo)
            
            if pago == "Pagado":
                if ruta_adjunto:
                    if os.path.exists(ruta_adjunto):
                        with open(ruta_adjunto, 'rb') as f:
                            pdf_data = f.read()
                        nombre_archivo = os.path.basename(ruta_adjunto)
                        msg.add_attachment(pdf_data, maintype='application', subtype='pdf', filename=nombre_archivo)
                
            server = smtplib.SMTP_SSL('smtp.gmail.com', 465)
            server.login(emisor, clave)
            server.send_message(msg)
            server.quit()
            return True
        except:
            return False