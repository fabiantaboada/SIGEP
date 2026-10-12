import customtkinter as ctk
import tkinter as tk
from tkinter import messagebox, filedialog
from PIL import Image
import os
import re
from datetime import datetime
from modelo import BaseDatos, ApiReniec, Configuracion
from logica import ControladorLogica

ctk.set_appearance_mode("Light")
ctk.set_default_color_theme("blue")
ruta_assets = os.path.join(os.path.dirname(__file__), 'assets')

class VentanaPrincipal(ctk.CTk):
    def __init__(self):
        super().__init__()
        self.title("SIGEP Workspace")
        self.minsize(1000, 650)
        
        self.after(0, lambda: self.state('zoomed'))
        self.configure(fg_color="#f8fafc")
        
        self.db = BaseDatos()
        self.logica = ControladorLogica()
        self.config = Configuracion()
        self.intentos_login = 0
        
        self.container = ctk.CTkFrame(self, fg_color="transparent")
        self.container.pack(fill="both", expand=True)
        self.mostrar_login()

    def limpiar_contenedor(self):
        hijos = self.container.winfo_children()
        for widget in hijos:
            widget.destroy()

    def adjuntar_validacion(self, entry_widget, var, tipo="texto"):
        def trace_func(*args):
            val = var.get()
            if tipo == "dni":
                clean_val = ''.join(filter(str.isdigit, val))
                if clean_val != val:
                    var.set(clean_val)
                    val = clean_val
                if len(val) > 8:
                    var.set(val[:8])
                    val = val[:8]
                if len(val) == 8:
                    entry_widget.configure(border_color="#10b981")
                else:
                    entry_widget.configure(border_color="#ef4444")
            elif tipo == "texto":
                clean_val = re.sub(r'[^A-Za-záéíóúÁÉÍÓÚñÑ\s\.\-]', '', val)
                if clean_val != val:
                    var.set(clean_val)
                    val = clean_val
                if len(val.strip()) >= 3:
                    entry_widget.configure(border_color="#10b981")
                else:
                    entry_widget.configure(border_color="#ef4444")
            elif tipo == "correo":
                if val == "":
                    entry_widget.configure(border_color="#cbd5e1")
                elif self.logica.validar_correo_regex(val):
                    entry_widget.configure(border_color="#10b981")
                else:
                    entry_widget.configure(border_color="#ef4444")
            elif tipo == "numero":
                clean_val = re.sub(r'[^0-9\.]', '', val)
                if clean_val != val:
                    var.set(clean_val)
                    val = clean_val
                if val.strip() != "":
                    entry_widget.configure(border_color="#10b981")
                else:
                    entry_widget.configure(border_color="#ef4444")
            elif tipo == "libre":
                if len(val.strip()) >= 1:
                    entry_widget.configure(border_color="#10b981")
                else:
                    entry_widget.configure(border_color="#ef4444")
            elif tipo == "fecha":
                clean_val = re.sub(r'[^0-9/]', '', val)
                if clean_val != val:
                    var.set(clean_val)
                    val = clean_val
                
                if len(val) > 10:
                    var.set(val[:10])
                    val = val[:10]
                    
                es_valido = True
                if val != "":
                    partes = val.split('/')
                    if len(partes) > 3:
                        es_valido = False
                        
                    if len(partes) >= 1:
                        if len(partes[0]) > 0:
                            if len(partes[0]) > 2:
                                es_valido = False
                            else:
                                try:
                                    dia = int(partes[0])
                                    if dia > 31:
                                        es_valido = False
                                    if dia == 0:
                                        es_valido = False
                                except ValueError:
                                    pass
                                    
                    if len(partes) >= 2:
                        if len(partes[1]) > 0:
                            if len(partes[1]) > 2:
                                es_valido = False
                            else:
                                try:
                                    mes = int(partes[1])
                                    if mes > 12:
                                        es_valido = False
                                    if mes == 0:
                                        es_valido = False
                                except ValueError:
                                    pass
                                    
                    if len(partes) >= 3:
                        if len(partes[2]) > 4:
                            es_valido = False
                            
                    if len(val) == 10:
                        if es_valido == True:
                            try:
                                datetime.strptime(val, "%d/%m/%Y")
                            except ValueError:
                                es_valido = False

                if val == "":
                    entry_widget.configure(border_color="#cbd5e1")
                elif es_valido == True:
                    entry_widget.configure(border_color="#10b981")
                else:
                    entry_widget.configure(border_color="#ef4444")
                    
        var.trace_add("write", trace_func)

    # ================= LOGIN DIRECTO =================
    def mostrar_login(self):
        self.limpiar_contenedor()
        
        f_login = ctk.CTkFrame(self.container, fg_color="white", width=850, height=550, corner_radius=15, border_width=1, border_color="#e2e8f0")
        f_login.pack_propagate(False)
        f_login.place(relx=0.5, rely=0.5, anchor="center")
        
        f_izq = ctk.CTkFrame(f_login, width=350, fg_color="#1e293b", corner_radius=15)
        f_izq.pack(side="left", fill="y")
        try:
            ruta_imglogin = os.path.join(ruta_assets, "imglogin.png")
            img_p = ctk.CTkImage(light_image=Image.open(ruta_imglogin), size=(350, 550))
            lbl_img = ctk.CTkLabel(f_izq, image=img_p, text="")
            lbl_img.pack(expand=True, fill="both")
        except:
            lbl_err = ctk.CTkLabel(f_izq, text="Imagen Lateral", text_color="white")
            lbl_err.pack(expand=True, fill="both")

        f_der = ctk.CTkFrame(f_login, fg_color="transparent")
        f_der.pack(side="right", fill="both", expand=True, padx=40, pady=30)
        
        f_centro_derecha = ctk.CTkFrame(f_der, fg_color="transparent")
        f_centro_derecha.pack(expand=True)
        
        f_logos = ctk.CTkFrame(f_centro_derecha, fg_color="transparent")
        f_logos.pack(anchor="center", pady=(0, 15))
        
        try:
            ruta_logo_sigep = os.path.join(ruta_assets, "logo.png")
            img_sigep = ctk.CTkImage(light_image=Image.open(ruta_logo_sigep), size=(110, 110))
            ctk.CTkLabel(f_logos, image=img_sigep, text="").pack(side="left", padx=(0, 20))
        except:
            pass
            
        try:
            ruta_logo_parroquia = os.path.join(ruta_assets, "parroquia.png")
            img_parroquia = ctk.CTkImage(light_image=Image.open(ruta_logo_parroquia), size=(110, 110))
            ctk.CTkLabel(f_logos, image=img_parroquia, text="").pack(side="left")
        except:
            pass
        
        ctk.CTkLabel(f_centro_derecha, text="Bienvenido a SIGEP", font=("Segoe UI", 26, "bold"), text_color="#0f172a").pack(anchor="center")
        ctk.CTkLabel(f_centro_derecha, text="Inicie sesión para continuar", font=("Segoe UI", 14), text_color="#64748b").pack(anchor="center", pady=(0, 30))
        
        f_inputs = ctk.CTkFrame(f_centro_derecha, fg_color="transparent")
        f_inputs.pack(anchor="center")

        var_usu = tk.StringVar()
        var_pas = tk.StringVar()
        
        ctk.CTkLabel(f_inputs, text="Usuario:", font=("Segoe UI", 12, "bold")).pack(anchor="w")
        txt_u = ctk.CTkEntry(f_inputs, textvariable=var_usu, width=320, height=45)
        txt_u.pack(pady=(0, 15))
        
        ctk.CTkLabel(f_inputs, text="Contraseña:", font=("Segoe UI", 12, "bold")).pack(anchor="w")
        txt_p = ctk.CTkEntry(f_inputs, textvariable=var_pas, show="*", width=320, height=45)
        txt_p.pack(pady=(0, 25))
        
        self.adjuntar_validacion(txt_u, var_usu, "libre")
        self.adjuntar_validacion(txt_p, var_pas, "libre")

        btn_ingresar = ctk.CTkButton(f_inputs, text="Ingresar al sistema", width=320, height=45, font=("Segoe UI", 14, "bold"), state="disabled")
        btn_ingresar.pack(pady=10)
        
        def check_login_inputs(*args):
            if len(var_usu.get()) > 0 and len(var_pas.get()) > 0:
                btn_ingresar.configure(state="normal")
            else:
                btn_ingresar.configure(state="disabled")
                
        var_usu.trace_add("write", check_login_inputs)
        var_pas.trace_add("write", check_login_inputs)

        def intentar_login():
            if self.logica.validar_credenciales(var_usu.get(), var_pas.get()):
                self.intentos_login = 0
                self.construir_interfaz_principal()
            else:
                self.intentos_login += 1
                if self.intentos_login >= 3:
                    messagebox.showerror("Seguridad", "Límite de intentos superado. El sistema se cerrará.")
                    self.destroy()
                else:
                    intentos_rest = 3 - self.intentos_login
                    messagebox.showerror("Acceso Denegado", f"Credenciales incorrectas. Quedan {intentos_rest} intentos.")
                    
        btn_ingresar.configure(command=intentar_login)

    # ================= SIDEBAR =================
    def construir_interfaz_principal(self):
        self.limpiar_contenedor()
        self.sidebar = ctk.CTkFrame(self.container, width=250, fg_color="#1e293b", corner_radius=0)
        self.sidebar.pack(side="left", fill="y")
        try:
            ruta_logo_blanco = os.path.join(ruta_assets, "logoblanco.png")
            logo = ctk.CTkImage(light_image=Image.open(ruta_logo_blanco), size=(85, 85))
            ctk.CTkLabel(self.sidebar, image=logo, text="").pack(pady=(30, 40))
        except:
            pass
            
        def ir_dashboard():
            self.mostrar_dashboard()
            
        def ir_registro():
            self.mostrar_registro()
            
        def ir_buscar():
            self.mostrar_buscar()
            
        def ir_reportes():
            self.mostrar_reportes()
            
        def ir_config():
            self.mostrar_configuracion()
            
        def cerrar_sesion():
            self.mostrar_login()
        
        ctk.CTkButton(self.sidebar, text="📊 Panel de Inicio", anchor="w", fg_color="transparent", hover_color="#334155", font=("Segoe UI", 14), command=ir_dashboard).pack(fill="x", padx=15, pady=5)
        ctk.CTkButton(self.sidebar, text="📝 Nuevo Expediente", anchor="w", fg_color="transparent", hover_color="#334155", font=("Segoe UI", 14), command=ir_registro).pack(fill="x", padx=15, pady=5)
        ctk.CTkButton(self.sidebar, text="🔍 Buscar y Editar", anchor="w", fg_color="transparent", hover_color="#334155", font=("Segoe UI", 14), command=ir_buscar).pack(fill="x", padx=15, pady=5)
        ctk.CTkButton(self.sidebar, text="📑 Reporte de Expedientes", anchor="w", fg_color="transparent", hover_color="#334155", font=("Segoe UI", 14), command=ir_reportes).pack(fill="x", padx=15, pady=5)
        ctk.CTkButton(self.sidebar, text="⚙ Configuración de Fechas", anchor="w", fg_color="transparent", hover_color="#334155", font=("Segoe UI", 14), command=ir_config).pack(fill="x", padx=15, pady=5)
        ctk.CTkButton(self.sidebar, text="Cerrar Sesión", fg_color="#ef4444", hover_color="#b91c1c", command=cerrar_sesion).pack(side="bottom", fill="x", pady=20, padx=20)

        self.content_frame = ctk.CTkFrame(self.container, fg_color="#f8fafc", corner_radius=0)
        self.content_frame.pack(side="right", fill="both", expand=True)
        self.mostrar_dashboard()

    def limpiar_contenido(self):
        hijos = self.content_frame.winfo_children()
        for widget in hijos:
            widget.destroy()

    # ================= DASHBOARD =================
    def mostrar_dashboard(self):
        self.limpiar_contenido()
        ctk.CTkLabel(self.content_frame, text="Hola, Bienvenida", font=("Segoe UI", 28, "bold"), text_color="#0f172a").pack(anchor="w", padx=40, pady=(40, 5))
        ctk.CTkLabel(self.content_frame, text="Resumen de Operaciones - Parroquia San José", font=("Segoe UI", 14), text_color="#64748b").pack(anchor="w", padx=40, pady=(0, 30))
        
        f_cards = ctk.CTkFrame(self.content_frame, fg_color="transparent")
        f_cards.pack(fill="x", padx=40)
        
        total = len(self.db.nombres)
        card1 = ctk.CTkFrame(f_cards, fg_color="white", border_width=1, border_color="#cbd5e1", height=120)
        card1.pack(side="left", fill="x", expand=True, padx=(0, 15))
        ctk.CTkLabel(card1, text="EXPEDIENTES TOTALES", font=("Segoe UI", 12, "bold"), text_color="#10b981").pack(anchor="w", padx=20, pady=(20, 5))
        ctk.CTkLabel(card1, text=str(total), font=("Segoe UI", 32, "bold"), text_color="#0f172a").pack(anchor="w", padx=20)

    # ================= CONFIGURACION =================
    def mostrar_configuracion(self):
        self.limpiar_contenido()
        ctk.CTkLabel(self.content_frame, text="Configuración de Fechas de Catequesis", font=("Segoe UI", 24, "bold"), text_color="#2563eb").pack(anchor="w", padx=40, pady=(30, 20))
        f_conf = ctk.CTkFrame(self.content_frame, fg_color="white", border_width=1, border_color="#cbd5e1")
        f_conf.pack(fill="x", padx=40, pady=10)
        
        ctk.CTkLabel(f_conf, text="Selecciona la fecha de inicio (Día / Mes)", font=("Segoe UI", 14, "bold")).pack(anchor="w", padx=20, pady=(20, 10))
        
        partes_comunion = self.config.fecha_comunion.split("/")
        c_dia = "--"
        c_mes = "--"
        if len(partes_comunion) == 2:
            c_dia = partes_comunion[0]
            c_mes = partes_comunion[1]
        
        f_1 = ctk.CTkFrame(f_conf, fg_color="transparent")
        f_1.pack(fill="x", padx=20, pady=10)
        ctk.CTkLabel(f_1, text="Primera Comunión:", width=200, anchor="w").pack(side="left")
        
        lista_dias = []
        lista_dias.append("--")
        for i in range(1,32):
            lista_dias.append(str(i).zfill(2))
            
        lista_meses = []
        lista_meses.append("--")
        for i in range(1,13):
            lista_meses.append(str(i).zfill(2))
            
        cb_d_com = ctk.CTkComboBox(f_1, values=lista_dias, width=80, state="readonly")
        cb_d_com.set(c_dia)
        cb_d_com.pack(side="left", padx=5)
        
        cb_m_com = ctk.CTkComboBox(f_1, values=lista_meses, width=80, state="readonly")
        cb_m_com.set(c_mes)
        cb_m_com.pack(side="left", padx=5)

        partes_confirmacion = self.config.fecha_confirmacion.split("/")
        cf_dia = "--"
        cf_mes = "--"
        if len(partes_confirmacion) == 2:
            cf_dia = partes_confirmacion[0]
            cf_mes = partes_confirmacion[1]
        
        f_2 = ctk.CTkFrame(f_conf, fg_color="transparent")
        f_2.pack(fill="x", padx=20, pady=10)
        ctk.CTkLabel(f_2, text="Confirmación:", width=200, anchor="w").pack(side="left")
        
        cb_d_con = ctk.CTkComboBox(f_2, values=lista_dias, width=80, state="readonly")
        cb_d_con.set(cf_dia)
        cb_d_con.pack(side="left", padx=5)
        
        cb_m_con = ctk.CTkComboBox(f_2, values=lista_meses, width=80, state="readonly")
        cb_m_con.set(cf_mes)
        cb_m_con.pack(side="left", padx=5)
        
        def guardar_config():
            dia_com = cb_d_com.get()
            mes_com = cb_m_com.get()
            dia_con = cb_d_con.get()
            mes_con = cb_m_con.get()

            now = datetime.now()
            current_year = now.year

            if dia_com != "--" or mes_com != "--":
                if dia_com == "--" or mes_com == "--":
                    messagebox.showerror("Error", "Para Primera Comunión, debe seleccionar día y mes, o dejar ambos en '--'.")
                    return
                try:
                    fecha_com_obj = datetime(current_year, int(mes_com), int(dia_com)).date()
                    if fecha_com_obj < now.date():
                        messagebox.showerror("Error", "La fecha de Primera Comunión configurada es anterior al día de hoy.")
                        return
                except ValueError:
                    messagebox.showerror("Error", "La fecha de Primera Comunión no existe en el calendario.")
                    return

            if dia_con != "--" or mes_con != "--":
                if dia_con == "--" or mes_con == "--":
                    messagebox.showerror("Error", "Para Confirmación, debe seleccionar día y mes, o dejar ambos en '--'.")
                    return
                try:
                    fecha_con_obj = datetime(current_year, int(mes_con), int(dia_con)).date()
                    if fecha_con_obj < now.date():
                        messagebox.showerror("Error", "La fecha de Confirmación configurada es anterior al día de hoy.")
                        return
                except ValueError:
                    messagebox.showerror("Error", "La fecha de Confirmación no existe en el calendario.")
                    return

            if dia_com == "--":
                if dia_con == "--":
                    messagebox.showerror("Error", "Debe configurar al menos una de las fechas (Primera Comunión o Confirmación).")
                    return

            self.config.fecha_comunion = f"{dia_com}/{mes_com}"
            self.config.fecha_confirmacion = f"{dia_con}/{mes_con}"
            self.config.guardar()
            messagebox.showinfo("Éxito", "Fechas actualizadas correctamente.")
            
        ctk.CTkButton(f_conf, text="Guardar Cambios", fg_color="#10b981", command=guardar_config).pack(pady=20)

    # ================= REGISTRO COMPLETO =================
    def mostrar_registro(self):
        self.limpiar_contenido()
        ctk.CTkLabel(self.content_frame, text="Registro Documentario", font=("Segoe UI", 24, "bold"), text_color="#2563eb").pack(anchor="w", padx=40, pady=(15, 10))
        f_main = ctk.CTkScrollableFrame(self.content_frame, fg_color="white", border_width=1, border_color="#cbd5e1")
        f_main.pack(fill="both", expand=True, padx=40, pady=(0, 20))
        
        f_top = ctk.CTkFrame(f_main, fg_color="transparent")
        f_top.pack(fill="x", padx=30, pady=10)
        ctk.CTkLabel(f_top, text="Trámite:", font=("Segoe UI", 12, "bold")).pack(side="left", padx=(0, 10))
        cb_tipo = ctk.CTkComboBox(f_top, values=["Bautizo", "Primera Comunión", "Confirmación", "Matrimonio Regular"], width=200, state="readonly")
        cb_tipo.pack(side="left")
        
        f_gen = ctk.CTkFrame(f_main, fg_color="transparent")
        f_gen.pack(fill="x", padx=30, pady=(0, 5))
        ctk.CTkLabel(f_gen, text="Género Titular:", font=("Segoe UI", 12, "bold"), width=120, anchor="w").pack(side="left")
        cb_gen = ctk.CTkComboBox(f_gen, values=["Femenino", "Masculino"], width=150, state="readonly")
        cb_gen.pack(side="left")
        
        var_dni1 = tk.StringVar()
        var_dni2 = tk.StringVar()
        var_nom1 = tk.StringVar()
        var_nom2 = tk.StringVar()
        var_correo1 = tk.StringVar()
        var_correo2 = tk.StringVar()
        
        f_datos = ctk.CTkFrame(f_main, fg_color="transparent")
        f_datos.pack(fill="x", padx=30)
        
        f_fila1 = ctk.CTkFrame(f_datos, fg_color="transparent")
        f_fila1.pack(fill="x", pady=5)
        lbl_d1 = ctk.CTkLabel(f_fila1, text="DNI Titular:", font=("Segoe UI", 12, "bold"), width=130, anchor="e")
        lbl_d1.pack(side="left", padx=5)
        txt_d1 = ctk.CTkEntry(f_fila1, textvariable=var_dni1, width=120)
        txt_d1.pack(side="left", padx=5)
        txt_nom1 = ctk.CTkEntry(f_fila1, textvariable=var_nom1, width=250, placeholder_text="Nombre Titular")
        txt_nom1.pack(side="left", padx=5)
        
        self.adjuntar_validacion(txt_d1, var_dni1, "dni")
        self.adjuntar_validacion(txt_nom1, var_nom1, "texto")

        f_fila2 = ctk.CTkFrame(f_datos, fg_color="transparent")
        f_fila2.pack(fill="x", pady=5)
        lbl_d2 = ctk.CTkLabel(f_fila2, text="DNI Apoderado:", font=("Segoe UI", 12, "bold"), width=130, anchor="e")
        lbl_d2.pack(side="left", padx=5)
        txt_d2 = ctk.CTkEntry(f_fila2, textvariable=var_dni2, width=120)
        txt_d2.pack(side="left", padx=5)
        txt_nom2 = ctk.CTkEntry(f_fila2, textvariable=var_nom2, width=250, placeholder_text="Nombre Apoderado")
        txt_nom2.pack(side="left", padx=5) 
        
        self.adjuntar_validacion(txt_d2, var_dni2, "dni")
        self.adjuntar_validacion(txt_nom2, var_nom2, "texto")

        def auto_buscar_dni(var_dni, txt_n_widget, var_nom):
            val = var_dni.get()
            if len(val) == 8:
                if val.isdigit():
                    txt_n_widget.delete(0, 'end')
                    txt_n_widget.insert(0, "Buscando Reniec...")
                    self.update()
                    res = ApiReniec.consultar_dni(val)
                    txt_n_widget.delete(0, 'end')
                    if res != "":
                        txt_n_widget.insert(0, res)
                    else:
                        messagebox.showinfo("Aviso", "DNI no encontrado. Ingrese manualmente el nombre.")
                        
        var_dni1.trace_add("write", lambda *args: auto_buscar_dni(var_dni1, txt_nom1, var_nom1))
        
        def trace_dni2(*args):
            if cb_tipo.get() == "Matrimonio Regular":
                auto_buscar_dni(var_dni2, txt_nom2, var_nom2)
            else:
                auto_buscar_dni(var_dni2, txt_nom2, var_nom2)
                
        var_dni2.trace_add("write", trace_dni2)

        f_opcion_correo = ctk.CTkFrame(f_datos, fg_color="transparent")
        ctk.CTkLabel(f_opcion_correo, text="Notificar a:", font=("Segoe UI", 12, "bold"), width=130, anchor="e").pack(side="left", padx=5)
        cb_opc_cor = ctk.CTkComboBox(f_opcion_correo, values=["Solo Novio", "Solo Novia", "Ambos"], state="readonly", width=150)
        cb_opc_cor.pack(side="left", padx=5)

        f_fila_correo = ctk.CTkFrame(f_datos, fg_color="transparent")
        f_fila_correo.pack(fill="x", pady=5)
        lbl_cor1 = ctk.CTkLabel(f_fila_correo, text="Correo Notif.:", font=("Segoe UI", 12, "bold"), width=130, anchor="e")
        lbl_cor1.pack(side="left", padx=5)
        txt_cor1 = ctk.CTkEntry(f_fila_correo, textvariable=var_correo1, width=250)
        txt_cor1.pack(side="left", padx=5)
        lbl_cor2 = ctk.CTkLabel(f_fila_correo, text="Correo Novia:", font=("Segoe UI", 12, "bold"), width=100, anchor="e")
        txt_cor2 = ctk.CTkEntry(f_fila_correo, textvariable=var_correo2, width=250)
        
        self.adjuntar_validacion(txt_cor1, var_correo1, "correo")
        self.adjuntar_validacion(txt_cor2, var_correo2, "correo")

        def cambiar_modo_correo(opc):
            lbl_cor2.pack_forget()
            txt_cor2.pack_forget()
            
            if cb_tipo.get() == "Matrimonio Regular":
                if opc == "Solo Novio":
                    lbl_cor1.configure(text="Correo Novio:")
                elif opc == "Solo Novia":
                    lbl_cor1.configure(text="Correo Novia:") 
                elif opc == "Ambos":
                    lbl_cor1.configure(text="Correo Novio:")
                    lbl_cor2.pack(side="left", padx=5)
                    txt_cor2.pack(side="left", padx=5)
            else:
                lbl_cor1.configure(text="Correo Notif.:")
                
        cb_opc_cor.configure(command=cambiar_modo_correo)

        f_req = ctk.CTkFrame(f_main, fg_color="#f8fafc", border_width=1, border_color="#e2e8f0")
        f_req.pack(fill="x", padx=30, pady=10)
        ctk.CTkLabel(f_req, text="Requisitos de Documentación:", font=("Segoe UI", 12, "bold")).grid(row=0, column=0, columnspan=2, padx=20, pady=(10, 5), sticky="w")
        
        self.chk_vars = []
        self.chks = []
        for i in range(8):
            var = tk.BooleanVar()
            chk = ctk.CTkCheckBox(f_req, text="", variable=var)
            self.chk_vars.append(var)
            self.chks.append(chk)

        f_pago = ctk.CTkFrame(f_main, fg_color="transparent")
        f_pago.pack(fill="x", padx=30, pady=5)
        ctk.CTkLabel(f_pago, text="Estado de Pago:", font=("Segoe UI", 12, "bold"), width=130, anchor="e").pack(side="left", padx=5)
        cb_pago = ctk.CTkComboBox(f_pago, values=["Pagado", "Falta Pagar"], width=150, state="readonly")
        cb_pago.set("Pagado")
        cb_pago.pack(side="left")
        
        var_deu = tk.StringVar()
        txt_deuda = ctk.CTkEntry(f_pago, textvariable=var_deu, width=150, placeholder_text="Monto deuda S/") 
        self.adjuntar_validacion(txt_deuda, var_deu, "numero")

        f_obs_container = ctk.CTkFrame(f_main, fg_color="transparent")
        f_obs_container.pack(fill="x", padx=30, pady=10)
        
        f_obs_izq = ctk.CTkFrame(f_obs_container, fg_color="#f1f5f9", border_width=1, border_color="#cbd5e1")
        f_obs_izq.pack(side="left", fill="both", expand=True, padx=(0, 10))
        ctk.CTkLabel(f_obs_izq, text="Observación Generada:", font=("Segoe UI", 12, "bold")).pack(anchor="w", padx=15, pady=(10,0))
        txt_obs_auto = ctk.CTkTextbox(f_obs_izq, height=120)
        txt_obs_auto.pack(fill="both", expand=True, padx=15, pady=10)
        txt_obs_auto.configure(state="disabled")

        f_obs_der = ctk.CTkFrame(f_obs_container, fg_color="#f1f5f9", border_width=1, border_color="#cbd5e1")
        f_obs_der.pack(side="right", fill="both", expand=True, padx=(10, 0))
        var_check_adic = tk.BooleanVar(value=False)
        chk_adic = ctk.CTkCheckBox(f_obs_der, text="¿Añadir observación adicional manual?", font=("Segoe UI", 12, "bold"), variable=var_check_adic)
        chk_adic.pack(anchor="w", padx=15, pady=(10, 0))
        txt_obs_adic = ctk.CTkTextbox(f_obs_der, height=120)

        def actualizar_obs_auto(*args):
            faltantes = []
            if hasattr(self, 'reqs_activos'):
                for i in range(self.reqs_activos):
                    if not self.chk_vars[i].get():
                        texto_limpio = self.chks[i].cget("text")[3:]
                        faltantes.append(texto_limpio)
            
            texto = ""
            if len(faltantes) > 0:
                unidos = "\n- ".join(faltantes)
                texto = f"Faltan los siguientes documentos físicos:\n- {unidos}\n"
            else:
                texto = "Documentación física completa.\n"
                
            if cb_pago.get() == "Falta Pagar":
                texto = texto + "Pendiente de pago por regularizar."
            
            txt_obs_auto.configure(state="normal")
            txt_obs_auto.delete("0.0", "end")
            txt_obs_auto.insert("0.0", texto.strip())
            txt_obs_auto.configure(state="disabled")

        for var in self.chk_vars:
            var.trace_add("write", actualizar_obs_auto)
            
        def toggle_deuda(valor):
            if valor == "Falta Pagar":
                txt_deuda.pack(side="left", padx=10)
            else:
                txt_deuda.pack_forget()
                var_deu.set("")
            actualizar_obs_auto()
            
        cb_pago.configure(command=toggle_deuda)
        var_deu.trace_add("write", actualizar_obs_auto)

        def toggle_obs_adic(*args):
            if var_check_adic.get():
                txt_obs_adic.pack(fill="both", expand=True, padx=15, pady=10)
            else:
                txt_obs_adic.pack_forget()
                txt_obs_adic.delete("0.0", "end")
                
        var_check_adic.trace_add("write", toggle_obs_adic)

        def actualizar_interfaz_registro(eleccion):
            reqs = []
            if eleccion == "Bautizo":
                reqs = ["1. Copia DNI Bautizado", "2. Partida Nacimiento", "3. Copia DNI Padre", "4. Copia DNI Madre", "5. Copia DNI Padrino", "6. Copia DNI Madrina"]
                f_gen.pack(fill="x", padx=30, after=f_top)
                lbl_d1.configure(text="DNI Bautizado:")
                txt_nom1.configure(placeholder_text="Nombre Bautizado")
                lbl_d2.configure(text="DNI Apoderado:")
                txt_nom2.configure(placeholder_text="Nombre Apoderado")
                f_opcion_correo.pack_forget()
                cambiar_modo_correo("N/A")
            elif eleccion == "Primera Comunión":
                reqs = ["1. Copia DNI Comulgante", "2. Partida Bautismo", "3. Copia DNI Padre", "4. Copia DNI Madre"]
                f_gen.pack(fill="x", padx=30, after=f_top)
                lbl_d1.configure(text="DNI Comulgante:")
                txt_nom1.configure(placeholder_text="Nombre Comulgante")
                lbl_d2.configure(text="DNI Apoderado:")
                txt_nom2.configure(placeholder_text="Nombre Apoderado")
                f_opcion_correo.pack_forget()
                cambiar_modo_correo("N/A")
            elif eleccion == "Confirmación":
                reqs = ["1. Copia DNI Confirmando", "2. Partida Bautismo", "3. Const. 1ra Comunión", "4. Copia DNI Padrino/Madrina", "5. Const. Sacram Padrino"]
                f_gen.pack(fill="x", padx=30, after=f_top)
                lbl_d1.configure(text="DNI Confirmando:")
                txt_nom1.configure(placeholder_text="Nombre Confirmando")
                lbl_d2.configure(text="DNI Apoderado:")
                txt_nom2.configure(placeholder_text="Nombre Apoderado")
                f_opcion_correo.pack_forget()
                cambiar_modo_correo("N/A")
            elif eleccion == "Matrimonio Regular":
                reqs = ["1. Copia DNI Novio", "2. Copia DNI Novia", "3. Partida Bautismo Novio", "4. Partida Bautismo Novia", "5. Confirmación Novio", "6. Confirmación Novia", "7. Copia DNI Testigo 1", "8. Copia DNI Testigo 2"]
                f_gen.pack_forget()
                lbl_d1.configure(text="DNI Novio:")
                txt_nom1.configure(placeholder_text="Nombre Novio")
                lbl_d2.configure(text="DNI Novia:")
                txt_nom2.configure(placeholder_text="Nombre Novia")
                f_opcion_correo.pack(fill="x", pady=5, before=f_fila_correo)
                cb_opc_cor.set("Ambos")
                cambiar_modo_correo("Ambos")
                
            self.reqs_activos = len(reqs)
            for j in range(len(self.chks)):
                chk = self.chks[j]
                if j < len(reqs):
                    chk.configure(text=reqs[j])
                    modulo = j % 2
                    fila = (j // 2) + 1
                    chk.grid(row=fila, column=modulo, sticky="w", padx=20, pady=5)
                else:
                    chk.grid_remove()
            actualizar_obs_auto()

        cb_tipo.configure(command=actualizar_interfaz_registro)
        cb_tipo.set("Bautizo")
        actualizar_interfaz_registro("Bautizo") 

        def procesar_guardado():
            dni1_val = var_dni1.get().strip()
            dni2_val = var_dni2.get().strip()
            nom1_val = var_nom1.get().strip()
            nom2_val = var_nom2.get().strip()
            
            if dni1_val == "" and dni2_val == "" and nom1_val == "" and nom2_val == "":
                messagebox.showerror("Error", "No se han llenado los datos del feligrés.")
                return

            errores = []
            if len(dni1_val) != 8:
                errores.append("- El DNI del titular debe tener exactamente 8 dígitos numéricos.")
            if nom1_val == "":
                errores.append("- Falta ingresar el Nombre del titular.")

            if cb_tipo.get() == "Primera Comunión":
                if self.config.fecha_comunion == "--/--":
                    errores.append("- Debe configurar la fecha de inicio de Catequesis en la sección 'Configuración de Fechas' antes de registrar.")
            
            if cb_tipo.get() == "Confirmación":
                if self.config.fecha_confirmacion == "--/--":
                    errores.append("- Debe configurar la fecha de inicio de Catequesis en la sección 'Configuración de Fechas' antes de registrar.")

            nom_final = ""
            gen_final = ""
            cor_final = ""
            
            if cb_tipo.get() == "Matrimonio Regular":
                if len(dni2_val) != 8:
                    errores.append("- El DNI de la Novia debe tener exactamente 8 dígitos numéricos.")
                if nom2_val == "":
                    errores.append("- Falta ingresar el Nombre de la Novia.")
                if dni1_val == dni2_val:
                    errores.append("- Los DNI de los novios no pueden ser iguales.")
                if nom1_val.lower() == nom2_val.lower():
                    errores.append("- Los nombres de los novios no pueden ser iguales.")
                    
                nom_final = nom1_val
                gen_final = "N/A"
                
                correos_validos = []
                opc = cb_opc_cor.get()
                
                if opc == "Solo Novio" or opc == "Ambos":
                    val_c1 = var_correo1.get().strip()
                    if val_c1 != "":
                        if not self.logica.validar_correo_regex(val_c1):
                            errores.append("- Correo del Novio inválido.")
                        else:
                            correos_validos.append(val_c1)
                            
                if opc == "Solo Novia" or opc == "Ambos":
                    val_c2 = var_correo2.get().strip()
                    if val_c2 != "":
                        if not self.logica.validar_correo_regex(val_c2):
                            errores.append("- Correo de la Novia inválido.")
                        else:
                            correos_validos.append(val_c2)
                            
                cor_final = ",".join(correos_validos)
            else:
                if len(dni2_val) != 8:
                    errores.append("- El DNI del Apoderado es obligatorio (8 dígitos numéricos).")
                if nom2_val == "":
                    errores.append("- Falta ingresar el Nombre del Apoderado.")
                    
                val_c1 = var_correo1.get().strip()
                if val_c1 != "":
                    if not self.logica.validar_correo_regex(val_c1):
                        errores.append("- El correo ingresado es inválido.")
                    else:
                        cor_final = val_c1
                        
                nom_final = nom1_val
                gen_final = cb_gen.get()

            pago = cb_pago.get()
            deuda = ""
            if pago == "Falta Pagar":
                deuda = txt_deuda.get().strip()
            else:
                deuda = "0"
                
            if pago == "Falta Pagar":
                if deuda == "":
                    errores.append("- Debe especificar el monto de la deuda.")
                else:
                    try:
                        monto_float = float(deuda)
                        if monto_float == 0:
                            errores.append("- El monto de la deuda no puede ser 0 si indica 'Falta Pagar'.")
                    except ValueError:
                        errores.append("- El monto de la deuda no es válido.")

            if len(errores) > 0:
                unidos = "\n".join(errores)
                messagebox.showerror("Datos Incompletos", f"Se detectaron los siguientes errores:\n\n{unidos}")
                return

            completos = True
            for k in range(self.reqs_activos):
                if not self.chk_vars[k].get():
                    completos = False
                    
            est = ""
            if completos:
                est = "Inscrito(a)"
            else:
                est = "Observado"
                
            obs_auto_txt = txt_obs_auto.get("0.0", "end").strip()
            
            obs_adic_txt = ""
            if var_check_adic.get():
                obs_adic_txt = txt_obs_adic.get("0.0", "end").strip()
            
            if completos == False:
                if var_check_adic.get() == True:
                    if obs_adic_txt == "":
                        messagebox.showwarning("Atención", "Ha marcado añadir observación adicional manual, pero la caja está vacía.")
                        return
                        
            res_conf = messagebox.askyesno("Confirmar", "¿Está seguro de inscribir y guardar este expediente?")
            if not res_conf:
                return

            lista_chks = []
            for k in range(self.reqs_activos):
                if self.chk_vars[k].get():
                    lista_chks.append("1")
                else:
                    lista_chks.append("0")
            chks_str = ",".join(lista_chks)
            
            self.db.registrar(dni1_val, dni2_val, nom_final, nom2_val, cor_final, gen_final, cb_tipo.get(), est, obs_auto_txt, obs_adic_txt, chks_str, "Por Programar", "---", "---", "---", pago, deuda)
            
            if pago == "Pagado":
                if completos:
                    dir_temp = os.path.join(os.path.dirname(__file__), 'exportaciones')
                    if not os.path.exists(dir_temp):
                        os.makedirs(dir_temp)
                        
                    ruta_pdf = os.path.join(dir_temp, f"Constancia_{dni1_val}.pdf")
                    
                    dni_impreso = ""
                    if cb_tipo.get() == "Matrimonio Regular":
                        dni_impreso = f"{dni1_val}, {dni2_val}"
                    else:
                        dni_impreso = dni1_val
                        
                    self.logica.generar_constancia_inscripcion(ruta_pdf, nom_final, nom2_val, dni_impreso, gen_final, cb_tipo.get(), self.config)

                    if cor_final != "":
                        self.logica.enviar_correo(cor_final, nom_final, nom2_val, cb_tipo.get(), est, obs_auto_txt, obs_adic_txt, pago, deuda, ruta_adjunto=ruta_pdf)
                        messagebox.showinfo("Éxito", "Inscrito Exitosamente. Constancia enviada a los correos.")
                    else:
                        messagebox.showinfo("Éxito", "Inscrito Exitosamente. Constancia guardada en 'exportaciones'.")
                else:
                    if cor_final != "":
                        self.logica.enviar_correo(cor_final, nom_final, nom2_val, cb_tipo.get(), est, obs_auto_txt, obs_adic_txt, pago, deuda)
                        messagebox.showinfo("Éxito", "Registrado. Se envió notificación al correo (Sin PDF).")
                    else:
                        messagebox.showinfo("Éxito", "Registrado correctamente (Pendiente de regularizar).")
            else:
                if cor_final != "":
                    self.logica.enviar_correo(cor_final, nom_final, nom2_val, cb_tipo.get(), est, obs_auto_txt, obs_adic_txt, pago, deuda)
                    messagebox.showinfo("Éxito", "Registrado. Se envió notificación al correo (Sin PDF).")
                else:
                    messagebox.showinfo("Éxito", "Registrado correctamente (Pendiente de regularizar).")
                    
            self.mostrar_dashboard()
            
        ctk.CTkButton(f_main, text="Inscribir y Guardar Expediente", font=("Segoe UI", 16, "bold"), fg_color="#2563eb", width=350, height=50, command=procesar_guardado).pack(pady=(20, 40))

    # ================= 7. BÚSQUEDA Y EDICIÓN =================
    def mostrar_buscar(self):
        self.limpiar_contenido()
        ctk.CTkLabel(self.content_frame, text="Buscador de Expedientes", font=("Segoe UI", 24, "bold"), text_color="#2563eb").pack(anchor="w", padx=40, pady=(15, 10))
        
        var_busq = tk.StringVar()
        f_top = ctk.CTkFrame(self.content_frame, fg_color="white", border_width=1, border_color="#cbd5e1")
        f_top.pack(fill="x", padx=40)
        ctk.CTkLabel(f_top, text="DNI del Feligrés:").pack(side="left", padx=20, pady=15)
        txt_bus = ctk.CTkEntry(f_top, textvariable=var_busq, width=250)
        txt_bus.pack(side="left", pady=15)
        self.adjuntar_validacion(txt_bus, var_busq, "dni")
        
        f_edit = ctk.CTkScrollableFrame(self.content_frame, fg_color="white", border_width=1, border_color="#cbd5e1")
        idx_temp = {"val": -1}
        
        f_fila_dni = ctk.CTkFrame(f_edit, fg_color="transparent")
        lbl_e_dni1 = ctk.CTkLabel(f_fila_dni, text="DNI Titular:", width=130, anchor="e")
        lbl_e_dni1.pack(side="left", padx=5)
        
        var_e_dni1 = tk.StringVar()
        t_dni1 = ctk.CTkEntry(f_fila_dni, textvariable=var_e_dni1, width=250)
        t_dni1.pack(side="left")
        self.adjuntar_validacion(t_dni1, var_e_dni1, "dni")
        
        lbl_e_dni2 = ctk.CTkLabel(f_fila_dni, text="DNI Apoderado:", width=130, anchor="e")
        lbl_e_dni2.pack(side="left", padx=5)
        
        var_e_dni2 = tk.StringVar()
        t_dni2 = ctk.CTkEntry(f_fila_dni, textvariable=var_e_dni2, width=250)
        t_dni2.pack(side="left")
        self.adjuntar_validacion(t_dni2, var_e_dni2, "dni")
        
        var_e_nom = tk.StringVar()
        var_e_nom2 = tk.StringVar()
        var_e_cor = tk.StringVar()
        
        f_fila_nom = ctk.CTkFrame(f_edit, fg_color="transparent")
        lbl_e_nom1 = ctk.CTkLabel(f_fila_nom, text="Titular:", width=130, anchor="e")
        lbl_e_nom1.pack(side="left", padx=5)
        t_nom = ctk.CTkEntry(f_fila_nom, textvariable=var_e_nom, width=250)
        t_nom.pack(side="left")
        lbl_e_nom2 = ctk.CTkLabel(f_fila_nom, text="Apoderado:", width=130, anchor="e")
        lbl_e_nom2.pack(side="left", padx=5)
        t_nom2 = ctk.CTkEntry(f_fila_nom, textvariable=var_e_nom2, width=250)
        t_nom2.pack(side="left")
        self.adjuntar_validacion(t_nom, var_e_nom, "texto")
        self.adjuntar_validacion(t_nom2, var_e_nom2, "texto")
        
        f_fila_cor = ctk.CTkFrame(f_edit, fg_color="transparent")
        ctk.CTkLabel(f_fila_cor, text="Correo(s):", width=130, anchor="e").pack(side="left", padx=5)
        t_cor = ctk.CTkEntry(f_fila_cor, textvariable=var_e_cor, width=400)
        t_cor.pack(side="left")
        self.adjuntar_validacion(t_cor, var_e_cor, "correo")
        
        f_fila_est = ctk.CTkFrame(f_edit, fg_color="transparent")
        ctk.CTkLabel(f_fila_est, text="Estado:", width=130, anchor="e").pack(side="left", padx=5)
        c_est = ctk.CTkComboBox(f_fila_est, values=["Inscrito(a)", "Observado", "Finalizado"], width=150, state="readonly")
        c_est.pack(side="left", padx=5)
        ctk.CTkLabel(f_fila_est, text="Pago:", width=50, anchor="e").pack(side="left", padx=5)
        c_pag = ctk.CTkComboBox(f_fila_est, values=["Pagado", "Falta Pagar"], width=120, state="readonly")
        c_pag.pack(side="left")
        
        var_e_deu = tk.StringVar()
        t_deu = ctk.CTkEntry(f_fila_est, textvariable=var_e_deu, width=80)
        self.adjuntar_validacion(t_deu, var_e_deu, "numero")
        
        f_req_edit = ctk.CTkFrame(f_edit, fg_color="#f8fafc", border_width=1, border_color="#e2e8f0")
        ctk.CTkLabel(f_req_edit, text="Requisitos de Documentación:", font=("Segoe UI", 12, "bold")).grid(row=0, column=0, columnspan=2, padx=20, pady=(5, 5), sticky="w")
        
        self.chk_vars_edit = []
        self.chks_edit = []
        for i in range(8):
            var = tk.BooleanVar()
            chk = ctk.CTkCheckBox(f_req_edit, text="", variable=var)
            self.chk_vars_edit.append(var)
            self.chks_edit.append(chk)

        f_obs_container_edit = ctk.CTkFrame(f_edit, fg_color="transparent")
        
        f_obs_izq_edit = ctk.CTkFrame(f_obs_container_edit, fg_color="#f1f5f9", border_width=1, border_color="#cbd5e1")
        f_obs_izq_edit.pack(side="left", fill="both", expand=True, padx=(0, 10))
        ctk.CTkLabel(f_obs_izq_edit, text="Observación Generada:", font=("Segoe UI", 12, "bold")).pack(anchor="w", padx=15, pady=(10,0))
        txt_obs_auto_edit = ctk.CTkTextbox(f_obs_izq_edit, height=120)
        txt_obs_auto_edit.pack(fill="both", expand=True, padx=15, pady=10)
        txt_obs_auto_edit.configure(state="disabled")
        
        f_obs_der_edit = ctk.CTkFrame(f_obs_container_edit, fg_color="#f1f5f9", border_width=1, border_color="#cbd5e1")
        f_obs_der_edit.pack(side="right", fill="both", expand=True, padx=(10, 0))
        var_chk_adic_edit = tk.BooleanVar(value=False)
        chk_adic_edit = ctk.CTkCheckBox(f_obs_der_edit, text="¿Añadir/Ver observación adicional manual?", font=("Segoe UI", 12, "bold"), variable=var_chk_adic_edit)
        chk_adic_edit.pack(anchor="w", padx=15, pady=(10, 0))
        txt_obs_adic_edit = ctk.CTkTextbox(f_obs_der_edit, height=120)

        def actualizar_obs_auto_edit(*args):
            faltantes = []
            if hasattr(self, 'reqs_activos_edit'):
                for j in range(self.reqs_activos_edit):
                    if not self.chk_vars_edit[j].get():
                        texto_limpio = self.chks_edit[j].cget("text")[3:]
                        faltantes.append(texto_limpio)
                        
            texto = ""
            if len(faltantes) > 0:
                unidos = "\n- ".join(faltantes)
                texto = f"Faltan los siguientes documentos físicos:\n- {unidos}\n"
            else:
                texto = "Documentación física completa.\n"
                
            if c_pag.get() == "Falta Pagar":
                texto = texto + "Pendiente de pago por regularizar."
            
            txt_obs_auto_edit.configure(state="normal")
            txt_obs_auto_edit.delete("0.0", "end")
            txt_obs_auto_edit.insert("0.0", texto.strip())
            txt_obs_auto_edit.configure(state="disabled")
            
            if len(faltantes) == 0:
                if c_pag.get() == "Pagado":
                    if c_est.get() == "Observado":
                        c_est.set("Inscrito(a)")
            
            if len(faltantes) > 0 or c_pag.get() == "Falta Pagar":
                if c_est.get() == "Inscrito(a)":
                    c_est.set("Observado")
        
        for var in self.chk_vars_edit:
            var.trace_add("write", actualizar_obs_auto_edit)
        
        def toggle_deuda_edit(e):
            actualizar_obs_auto_edit()
            if e == "Falta Pagar":
                t_deu.pack(side="left", padx=5)
            else:
                t_deu.pack_forget()
                var_e_deu.set("")
                
        c_pag.configure(command=toggle_deuda_edit)
        var_e_deu.trace_add("write", actualizar_obs_auto_edit)
        
        def toggle_obs_adic_edit(*args):
            if var_chk_adic_edit.get():
                txt_obs_adic_edit.pack(fill="both", expand=True, padx=15, pady=10)
            else:
                txt_obs_adic_edit.pack_forget()
                txt_obs_adic_edit.delete("0.0", "end")
                
        var_chk_adic_edit.trace_add("write", toggle_obs_adic_edit)

        f_fila_arch = ctk.CTkFrame(f_edit, fg_color="transparent")
        
        var_e_fec = tk.StringVar()
        ctk.CTkLabel(f_fila_arch, text="Fecha Oficial:", width=130, anchor="e").pack(side="left", padx=5)
        t_fec = ctk.CTkEntry(f_fila_arch, textvariable=var_e_fec, width=150, placeholder_text="DD/MM/AAAA", placeholder_text_color="#94a3b8")
        t_fec.pack(side="left", padx=5)
        self.adjuntar_validacion(t_fec, var_e_fec, "fecha")
        
        ctk.CTkLabel(f_fila_arch, text="Libro:").pack(side="left", padx=5)
        t_l = ctk.CTkEntry(f_fila_arch, width=60, placeholder_text="---")
        t_l.pack(side="left")
        
        ctk.CTkLabel(f_fila_arch, text="Folio:").pack(side="left", padx=5)
        t_f = ctk.CTkEntry(f_fila_arch, width=60, placeholder_text="---")
        t_f.pack(side="left")
        
        ctk.CTkLabel(f_fila_arch, text="Asiento:").pack(side="left", padx=5)
        t_a = ctk.CTkEntry(f_fila_arch, width=60, placeholder_text="---")
        t_a.pack(side="left")
        
        f_btn = ctk.CTkFrame(f_edit, fg_color="transparent")
        btn_act = ctk.CTkButton(f_btn, text="Actualizar Datos", fg_color="#2563eb", width=180)
        btn_env = ctk.CTkButton(f_btn, text="Enviar por Correo", fg_color="#10b981", width=180)
        btn_eli = ctk.CTkButton(f_btn, text="Eliminar", fg_color="#ef4444", width=150)

        def buscar():
            dni_val = var_busq.get().strip()
            if dni_val == "":
                messagebox.showwarning("Error de Búsqueda", "No ha ingresado un DNI.")
                return
            if len(dni_val) != 8:
                messagebox.showwarning("Error de Búsqueda", "El DNI ingresado es incorrecto. Debe contener exactamente 8 dígitos numéricos.")
                return
                
            idx = self.db.buscar_por_dni(dni_val)
            if idx != -1:
                idx_temp["val"] = idx
                f_edit.pack(fill="both", expand=True, padx=40, pady=10)
                f_fila_dni.pack(fill="x", pady=10)
                f_fila_nom.pack(fill="x", pady=10)
                f_fila_cor.pack(fill="x", pady=10)
                f_fila_est.pack(fill="x", pady=10)
                
                tipo = self.db.tipos[idx]
                reqs = []
                if tipo == "Bautizo": 
                    reqs = ["1. Copia DNI Bautizado", "2. Partida Nacimiento", "3. Copia DNI Padre", "4. Copia DNI Madre", "5. Copia DNI Padrino", "6. Copia DNI Madrina"]
                    lbl_e_dni1.configure(text="DNI Bautizado:")
                    lbl_e_dni2.configure(text="DNI Apoderado:")
                    lbl_e_nom1.configure(text="Bautizado:")
                    lbl_e_nom2.configure(text="Apoderado:")
                elif tipo == "Primera Comunión": 
                    reqs = ["1. Copia DNI Comulgante", "2. Partida Bautismo", "3. Copia DNI Padre", "4. Copia DNI Madre"]
                    lbl_e_dni1.configure(text="DNI Comulgante:")
                    lbl_e_dni2.configure(text="DNI Apoderado:")
                    lbl_e_nom1.configure(text="Comulgante:")
                    lbl_e_nom2.configure(text="Apoderado:")
                elif tipo == "Confirmación": 
                    reqs = ["1. Copia DNI Confirmando", "2. Partida Bautismo", "3. Const. 1ra Comunión", "4. Copia DNI Padrino/Madrina", "5. Const. Sacram Padrino"]
                    lbl_e_dni1.configure(text="DNI Confirmando:")
                    lbl_e_dni2.configure(text="DNI Apoderado:")
                    lbl_e_nom1.configure(text="Confirmando:")
                    lbl_e_nom2.configure(text="Apoderado:")
                elif tipo == "Matrimonio Regular": 
                    reqs = ["1. Copia DNI Novio", "2. Copia DNI Novia", "3. Partida Bautismo Novio", "4. Partida Bautismo Novia", "5. Confirmación Novio", "6. Confirmación Novia", "7. Copia DNI Testigo 1", "8. Copia DNI Testigo 2"]
                    lbl_e_dni1.configure(text="DNI Novio:")
                    lbl_e_dni2.configure(text="DNI Novia:")
                    lbl_e_nom1.configure(text="Novio:")
                    lbl_e_nom2.configure(text="Novia:")
                
                self.reqs_activos_edit = len(reqs)
                memoria = self.db.checks_mem[idx].split(",")
                for k in range(len(self.chks_edit)):
                    chk = self.chks_edit[k]
                    if k < len(reqs):
                        chk.configure(text=reqs[k])
                        modulo = k % 2
                        fila = (k // 2) + 1
                        chk.grid(row=fila, column=modulo, sticky="w", padx=20, pady=5)
                        if k < len(memoria):
                            if memoria[k] == "1":
                                self.chk_vars_edit[k].set(True)
                            else:
                                self.chk_vars_edit[k].set(False)
                        else:
                            self.chk_vars_edit[k].set(False)
                    else:
                        chk.grid_remove()
                
                f_req_edit.pack(fill="x", pady=10)
                f_obs_container_edit.pack(fill="x", pady=10)
                f_fila_arch.pack(fill="x", pady=10)
                f_btn.pack(pady=15)
                
                t_dni1.delete(0, 'end')
                t_dni1.insert(0, self.db.dnis_prin[idx])
                
                t_dni2.delete(0, 'end')
                t_dni2.insert(0, self.db.dnis_sec[idx])
                
                t_nom.delete(0, 'end')
                t_nom.insert(0, self.db.nombres[idx])
                
                t_nom2.delete(0, 'end')
                t_nom2.insert(0, self.db.nombres_sec[idx])
                
                t_cor.delete(0, 'end')
                t_cor.insert(0, self.db.correos[idx])
                
                c_est.set(self.db.estados[idx])
                c_pag.set(self.db.pagos[idx])
                
                t_deu.delete(0, 'end')
                t_deu.insert(0, self.db.deudas[idx])
                
                if self.db.pagos[idx] == "Pagado":
                    t_deu.pack_forget()
                else:
                    t_deu.pack(side="left", padx=5)
                
                txt_obs_adic_edit.delete("0.0", "end")
                txt_obs_adic_edit.insert("0.0", self.db.obs_adic[idx])
                
                if self.db.obs_adic[idx] != "":
                    var_chk_adic_edit.set(True)
                else:
                    var_chk_adic_edit.set(False)
                
                t_fec.delete(0, 'end')
                if self.db.fechas_sac[idx] != "Por Programar":
                    t_fec.insert(0, self.db.fechas_sac[idx])
                
                t_l.delete(0, 'end')
                if self.db.libros[idx] != "---":
                    t_l.insert(0, self.db.libros[idx])
                    
                t_f.delete(0, 'end')
                if self.db.folios[idx] != "---":
                    t_f.insert(0, self.db.folios[idx])
                    
                t_a.delete(0, 'end')
                if self.db.asientos[idx] != "---":
                    t_a.insert(0, self.db.asientos[idx])
                
                btn_act.pack(side="left", padx=10)
                btn_env.pack(side="left", padx=10)
                btn_eli.pack(side="left", padx=10)
                actualizar_obs_auto_edit() 
                
                messagebox.showinfo("Búsqueda Exitosa", f"Expediente de {self.db.nombres[idx]} encontrado.")
            else:
                f_edit.pack_forget()
                messagebox.showwarning("Aviso", "No se encontraron expedientes con ese DNI.")

        ctk.CTkButton(f_top, text="Consultar", fg_color="#10b981", command=buscar).pack(side="left", padx=20)

        def ejecutar_accion(enviar_correo=False):
            dni1_val = t_dni1.get().strip()
            dni2_val = t_dni2.get().strip()
            
            if len(dni1_val) != 8:
                messagebox.showerror("Error", "El DNI principal debe tener 8 dígitos.")
                return
            if len(dni2_val) != 8:
                messagebox.showerror("Error", "El DNI secundario debe tener 8 dígitos.")
                return
                
            if t_nom.get().strip() == "":
                messagebox.showerror("Error", "El nombre del titular es obligatorio.")
                return
            if t_nom2.get().strip() == "":
                messagebox.showerror("Error", "El nombre secundario es obligatorio.")
                return
                
            correos_edit = t_cor.get()
            if correos_edit != "":
                lista_correos = correos_edit.split(',')
                for c in lista_correos:
                    if not self.logica.validar_correo_regex(c.strip()):
                        messagebox.showerror("Error", f"El correo '{c.strip()}' no es válido.")
                        return

            i = idx_temp["val"]
            
            if self.db.tipos[i] == "Primera Comunión":
                if self.config.fecha_comunion == "--/--":
                    messagebox.showerror("Error", "Debe configurar la fecha de inicio de Catequesis en la sección 'Configuración de Fechas' antes de actualizar.")
                    return
            
            if self.db.tipos[i] == "Confirmación":
                if self.config.fecha_confirmacion == "--/--":
                    messagebox.showerror("Error", "Debe configurar la fecha de inicio de Catequesis en la sección 'Configuración de Fechas' antes de actualizar.")
                    return

            f_v = t_fec.get().strip()
            if f_v == "":
                f_v = "Por Programar"
            else:
                try:
                    fecha_obj = datetime.strptime(f_v, "%d/%m/%Y").date()
                    fecha_hoy = datetime.now().date()
                    if fecha_obj < fecha_hoy:
                        messagebox.showerror("Error", "La Fecha Oficial no puede ser una fecha antigua (pasada).")
                        return
                except ValueError:
                    messagebox.showerror("Error", "El formato de la Fecha Oficial debe ser numérico: DD/MM/AAAA.")
                    return

            estado_elegido = c_est.get()
            pago_elegido = c_pag.get()
            
            deuda_nueva = ""
            if pago_elegido == "Falta Pagar":
                deuda_nueva = t_deu.get().strip()
            else:
                deuda_nueva = "0"
                
            if pago_elegido == "Falta Pagar":
                if deuda_nueva == "":
                    messagebox.showerror("Error", "Debe especificar el monto de la deuda.")
                    return
                try:
                    deuda_float = float(deuda_nueva)
                    if deuda_float == 0:
                        messagebox.showerror("Error", "El monto de la deuda no puede ser 0 si indica 'Falta Pagar'.")
                        return
                except ValueError:
                    messagebox.showerror("Error", "El monto de la deuda no es válido.")
                    return
                
            obs_auto_txt = txt_obs_auto_edit.get("0.0", "end").strip()
            
            obs_adic_txt = ""
            if var_chk_adic_edit.get():
                obs_adic_txt = txt_obs_adic_edit.get("0.0", "end").strip()

            completos = True
            for j in range(self.reqs_activos_edit):
                if not self.chk_vars_edit[j].get():
                    completos = False
            
            if completos == False:
                if var_chk_adic_edit.get() == True:
                    if obs_adic_txt == "":
                        messagebox.showwarning("Atención", "Ha marcado añadir observación adicional manual, pero la caja está vacía.")
                        return

            l_v = t_l.get().strip()
            if l_v == "":
                l_v = "---"
            
            f_val = t_f.get().strip()
            if f_val == "":
                f_val = "---"
            
            a_v = t_a.get().strip()
            if a_v == "":
                a_v = "---"
            
            lista_chks_e = []
            for j in range(self.reqs_activos_edit):
                if self.chk_vars_edit[j].get():
                    lista_chks_e.append("1")
                else:
                    lista_chks_e.append("0")
            chks_str = ",".join(lista_chks_e)
            
            if self.db.tipos[i] == "Matrimonio Regular":
                if dni1_val == dni2_val:
                    messagebox.showerror("Error", "Los DNI de los novios no pueden ser iguales.")
                    return
                if t_nom.get().strip().lower() == t_nom2.get().strip().lower():
                    messagebox.showerror("Error", "Los nombres de los novios no pueden ser iguales.")
                    return
            
            self.db.actualizar_completo(i, dni1_val, dni2_val, t_nom.get(), t_nom2.get(), t_cor.get(), self.db.generos[i], estado_elegido, obs_auto_txt, obs_adic_txt, chks_str, f_v, l_v, f_val, a_v, pago_elegido, deuda_nueva)
            
            if enviar_correo == False:
                messagebox.showinfo("Éxito", "Datos actualizados en el sistema.")
            else:
                if t_cor.get() == "":
                    messagebox.showwarning("Atención", "No hay ningún correo registrado. Los datos fueron guardados, pero no se enviará notificación.")
                    buscar()
                    return

                if pago_elegido == "Pagado":
                    if estado_elegido == "Inscrito(a)":
                        dir_temp = os.path.join(os.path.dirname(__file__), 'exportaciones')
                        if not os.path.exists(dir_temp):
                            os.makedirs(dir_temp)
                            
                        ruta_pdf = os.path.join(dir_temp, f"Constancia_{self.db.dnis_prin[i]}.pdf")
                        
                        dni_impreso = ""
                        if self.db.tipos[i] == "Matrimonio Regular":
                            dni_impreso = f"{self.db.dnis_prin[i]}, {self.db.dnis_sec[i]}"
                        else:
                            dni_impreso = self.db.dnis_prin[i]
                            
                        self.logica.generar_constancia_inscripcion(ruta_pdf, t_nom.get(), t_nom2.get(), dni_impreso, self.db.generos[i], self.db.tipos[i], self.config)
                        self.logica.enviar_correo(t_cor.get(), t_nom.get(), t_nom2.get(), self.db.tipos[i], estado_elegido, obs_auto_txt, obs_adic_txt, pago_elegido, deuda_nueva, ruta_adjunto=ruta_pdf)
                        messagebox.showinfo("Éxito", "Datos actualizados en el sistema y Constancia enviada a los correos.")

                    elif estado_elegido == "Finalizado":
                        dir_temp = os.path.join(os.path.dirname(__file__), 'exportaciones')
                        if not os.path.exists(dir_temp):
                            os.makedirs(dir_temp)
                            
                        ruta_pdf = os.path.join(dir_temp, f"Partida_Oficial_{self.db.dnis_prin[i]}.pdf")
                        self.logica.generar_partida_generica(ruta_pdf, t_nom.get(), t_nom2.get(), self.db.tipos[i], l_v, f_val, a_v)
                        self.logica.enviar_correo(t_cor.get(), t_nom.get(), t_nom2.get(), self.db.tipos[i], estado_elegido, obs_auto_txt, obs_adic_txt, pago_elegido, deuda_nueva, ruta_adjunto=ruta_pdf)
                        messagebox.showinfo("Éxito", "Datos actualizados en el sistema y Partida enviada por correo a los titulares.")
                    else:
                        self.logica.enviar_correo(t_cor.get(), t_nom.get(), t_nom2.get(), self.db.tipos[i], estado_elegido, obs_auto_txt, obs_adic_txt, pago_elegido, deuda_nueva)
                        messagebox.showinfo("Éxito", "Datos actualizados en el sistema y Notificación de actualización enviada (Sin PDF).")
                else:
                    self.logica.enviar_correo(t_cor.get(), t_nom.get(), t_nom2.get(), self.db.tipos[i], estado_elegido, obs_auto_txt, obs_adic_txt, pago_elegido, deuda_nueva)
                    messagebox.showinfo("Éxito", "Datos actualizados en el sistema y Notificación de actualización enviada (Sin PDF).")
            buscar() 

        def eliminar():
            res = messagebox.askyesno("Confirmar", "¿Está seguro de eliminar permanentemente el registro del sistema?")
            if res:
                self.db.eliminar(idx_temp["val"])
                f_edit.pack_forget()
                messagebox.showinfo("Eliminado", "Registro borrado correctamente.")

        def cmd_act():
            res = messagebox.askyesno("Confirmar", "¿Está seguro de actualizar los datos en el sistema?")
            if res:
                ejecutar_accion(False)
            
        def cmd_env():
            res = messagebox.askyesno("Confirmar", "¿Está seguro de actualizar los datos y enviar por correo?")
            if res:
                ejecutar_accion(True)

        btn_act.configure(command=cmd_act)
        btn_env.configure(command=cmd_env)
        btn_eli.configure(command=eliminar)

    # ================= 8. REPORTES =================
    def mostrar_reportes(self):
        self.limpiar_contenido()
        ctk.CTkLabel(self.content_frame, text="Reporte General de Expedientes", font=("Segoe UI", 24, "bold"), text_color="#2563eb").pack(anchor="w", padx=40, pady=(15, 10))
        
        f_filtros = ctk.CTkFrame(self.content_frame, fg_color="#f1f5f9", border_width=1, border_color="#cbd5e1")
        f_filtros.pack(fill="x", padx=40, pady=5)
        
        ctk.CTkLabel(f_filtros, text="Sacramento:", font=("Segoe UI", 12, "bold")).pack(side="left", padx=10, pady=10)
        cb_filtro_sac = ctk.CTkComboBox(f_filtros, values=["Todos", "Bautizo", "Primera Comunión", "Confirmación", "Matrimonio Regular"], width=150, state="readonly")
        cb_filtro_sac.set("Todos")
        cb_filtro_sac.pack(side="left", padx=5)
        
        f_lbl_fec = ctk.CTkFrame(f_filtros, fg_color="transparent")
        f_lbl_fec.pack(side="left", padx=(10, 5))
        ctk.CTkLabel(f_lbl_fec, text="Fecha Oficial:\n(DD/MM/AAAA)", font=("Segoe UI", 12, "bold")).pack(anchor="w")
        
        var_filtro_fec = tk.StringVar()
        txt_filtro_fec = ctk.CTkEntry(f_filtros, textvariable=var_filtro_fec, width=110)
        txt_filtro_fec.pack(side="left", padx=5)
        self.adjuntar_validacion(txt_filtro_fec, var_filtro_fec, "fecha")
        
        ctk.CTkLabel(f_filtros, text="Ordenar:", font=("Segoe UI", 12, "bold")).pack(side="left", padx=10)
        cb_orden = ctk.CTkComboBox(f_filtros, values=["Ascendente (A-Z)", "Descendente (Z-A)"], width=150, state="readonly")
        cb_orden.set("Ascendente (A-Z)")
        cb_orden.pack(side="left", padx=5)

        marco_tabla = ctk.CTkScrollableFrame(self.content_frame, fg_color="white", border_width=1, border_color="#cbd5e1")
        marco_tabla.pack(fill="both", expand=True, padx=40, pady=10)

        def actualizar_tabla():
            hijos = marco_tabla.winfo_children()
            for w in hijos:
                w.destroy()
            
            if cb_orden.get() == "Ascendente (A-Z)":
                self.db.ordenar_bubble_asc()
            else:
                self.db.ordenar_selection_desc()

            ctk.CTkLabel(marco_tabla, text="Titular(es)", font=("Segoe UI", 12, "bold"), width=300, anchor="w").grid(row=0, column=0, padx=10, pady=10)
            ctk.CTkLabel(marco_tabla, text="Sacramento", font=("Segoe UI", 12, "bold"), width=150, anchor="w").grid(row=0, column=1, padx=10, pady=10)
            ctk.CTkLabel(marco_tabla, text="Fecha Oficial", font=("Segoe UI", 12, "bold"), width=100, anchor="w").grid(row=0, column=2, padx=10, pady=10)
            ctk.CTkLabel(marco_tabla, text="Estado", font=("Segoe UI", 12, "bold"), width=100, anchor="w").grid(row=0, column=3, padx=10, pady=10)
            ctk.CTkLabel(marco_tabla, text="Deuda", font=("Segoe UI", 12, "bold"), width=100, anchor="w").grid(row=0, column=4, padx=10, pady=10)
            
            f_sac = cb_filtro_sac.get()
            f_fec = txt_filtro_fec.get().strip()
            fila = 1
            
            for i in range(len(self.db.nombres)):
                if f_sac != "Todos":
                    if self.db.tipos[i] != f_sac:
                        continue
                        
                if f_fec != "":
                    if self.db.fechas_sac[i] != f_fec:
                        continue

                nombre_display = ""
                if self.db.tipos[i] == "Matrimonio Regular":
                    nombre_display = f"{self.db.nombres[i]},\n{self.db.nombres_sec[i]}"
                else:
                    nombre_display = self.db.nombres[i]

                ctk.CTkLabel(marco_tabla, text=nombre_display, width=300, anchor="w", justify="left").grid(row=fila, column=0, padx=10, pady=5)
                ctk.CTkLabel(marco_tabla, text=self.db.tipos[i], width=150, anchor="w").grid(row=fila, column=1, padx=10, pady=5)
                ctk.CTkLabel(marco_tabla, text=self.db.fechas_sac[i], width=100, anchor="w").grid(row=fila, column=2, padx=10, pady=5)
                
                color_est = ""
                if self.db.estados[i] == "Finalizado":
                    color_est = "#10b981"
                elif self.db.estados[i] == "Inscrito(a)":
                    color_est = "#f59e0b"
                else:
                    color_est = "#ef4444"
                    
                texto_est = f"{self.db.estados[i]}\n({self.db.pagos[i]})"
                ctk.CTkLabel(marco_tabla, text=texto_est, width=100, anchor="w", text_color=color_est, justify="left").grid(row=fila, column=3, padx=10, pady=5)
                
                deuda_val = self.db.deudas[i]
                texto_deuda = ""
                if deuda_val == "0":
                    texto_deuda = "No tiene deuda"
                elif deuda_val == "":
                    texto_deuda = "No tiene deuda"
                else:
                    texto_deuda = f"S/ {deuda_val}"
                    
                color_deuda = ""
                if texto_deuda == "No tiene deuda":
                    color_deuda = "#10b981"
                else:
                    color_deuda = "#ef4444"
                    
                ctk.CTkLabel(marco_tabla, text=texto_deuda, width=100, anchor="w", text_color=color_deuda).grid(row=fila, column=4, padx=10, pady=5)
                
                fila += 1
                
            if fila == 1:
                ctk.CTkLabel(marco_tabla, text="No se encontraron expedientes con los filtros actuales.", text_color="#ef4444").grid(row=1, column=0, columnspan=5, pady=20)
                
        ctk.CTkButton(f_filtros, text="Aplicar Filtros", fg_color="#2563eb", width=120, command=actualizar_tabla).pack(side="left", padx=20)
        actualizar_tabla() 

        def exportar():
            if len(self.db.nombres) == 0:
                messagebox.showwarning("Vacío", "No hay expedientes registrados en el sistema para exportar.")
                return
            
            f_sac = cb_filtro_sac.get()
            f_fec = txt_filtro_fec.get().strip()
            filtrados = []
            
            for i in range(len(self.db.nombres)):
                if f_sac != "Todos":
                    if self.db.tipos[i] != f_sac:
                        continue
                        
                if f_fec != "":
                    if self.db.fechas_sac[i] != f_fec:
                        continue
                        
                nom = ""
                if self.db.tipos[i] == "Matrimonio Regular":
                    nom = f"{self.db.nombres[i]}, {self.db.nombres_sec[i]}"
                else:
                    nom = self.db.nombres[i]
                    
                deuda_val = self.db.deudas[i]
                texto_deuda = ""
                if deuda_val == "0":
                    texto_deuda = "No tiene deuda"
                elif deuda_val == "":
                    texto_deuda = "No tiene deuda"
                else:
                    texto_deuda = f"S/ {deuda_val}"
                    
                filtrados.append({"nom": nom, "tipo": self.db.tipos[i], "dni": self.db.dnis_prin[i], "est": self.db.estados[i], "deuda": texto_deuda})
            
            if len(filtrados) == 0:
                messagebox.showwarning("Vacío", "No hay expedientes que coincidan con los filtros actuales para exportar.")
                return
                
            ruta = filedialog.asksaveasfilename(defaultextension=".pdf", initialfile="Reporte_General.pdf", filetypes=[("PDF", "*.pdf")])
            if ruta:
                self.logica.generar_reporte_general_pdf(ruta, filtrados)

        ctk.CTkButton(self.content_frame, text="Exportar Resumen PDF", font=("Segoe UI", 14, "bold"), fg_color="#10b981", height=40, width=250, command=exportar).pack(pady=15)