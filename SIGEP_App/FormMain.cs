using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace SIGEP_App
{
    public partial class FormMain : Form
    {
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern Int32 SendMessage(IntPtr hWnd, int msg, int wParam, [MarshalAs(UnmanagedType.LPWStr)] string lParam);
        private const int EM_SETCUEBANNER = 0x1501;

        private void AsignarPlaceholder(TextBox caja, string texto)
        {
            if (caja.IsHandleCreated)
            {
                SendMessage(caja.Handle, EM_SETCUEBANNER, 0, texto);
            }
            else
            {
                caja.HandleCreated += (s, e) =>
                {
                    SendMessage(caja.Handle, EM_SETCUEBANNER, 0, texto);
                };
            }
        }

        private BaseDatos db;
        private ControladorLogica logica;
        private Configuracion config;
        private int intentos_login = 0;
        private bool sesion_iniciada = false;
        private string ruta_assets;

        private MenuStrip menuPrincipal;
        private ToolStripMenuItem itemIrInicio;
        private ToolStripMenuItem itemCerrarSesion;
        private ToolStripSeparator separadorArchivo;

        private Panel container;
        private Panel sidebar;
        private Panel content_frame;

        private List<CheckBox> chks = new List<CheckBox>();
        private int reqs_activos = 0;

        private List<CheckBox> chks_edit = new List<CheckBox>();
        private int reqs_activos_edit = 0;

        private Dictionary<Control, Color> coloresBorde = new Dictionary<Control, Color>();
        private HashSet<Control> panelesConPaint = new HashSet<Control>();

        public FormMain()
        {
            this.Text = "SIGEP Workspace - Sistema de Gestión Parroquial";
            this.MinimumSize = new Size(1050, 720);
            this.WindowState = FormWindowState.Maximized;
            this.BackColor = ColorTranslator.FromHtml("#f8fafc");
            this.Font = new Font("Segoe UI", 10f, FontStyle.Regular);

            this.ruta_assets = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "assets");
            this.db = new BaseDatos();
            this.logica = new ControladorLogica();
            this.config = new Configuracion();

            this.container = new Panel();
            this.container.Dock = DockStyle.Fill;
            this.container.BackColor = Color.Transparent;

            this.ConstruirMenuSuperior();

            this.Controls.Add(this.container);
            this.Controls.Add(this.menuPrincipal);
            this.MainMenuStrip = this.menuPrincipal;

            this.mostrar_login();
        }

        private Button CrearBotonAccion(string texto, Color colorFondo, Point ubicacion, Size tamano, float tamFuente = 10f)
        {
            Button btn = new Button();
            btn.Text = texto;
            btn.Location = ubicacion;
            btn.Size = tamano;
            btn.UseVisualStyleBackColor = false;
            btn.FlatStyle = FlatStyle.Flat;
            btn.FlatAppearance.BorderSize = 0;
            btn.BackColor = colorFondo;
            btn.ForeColor = Color.White;
            btn.Font = new Font("Segoe UI", tamFuente, FontStyle.Bold);
            btn.TextAlign = ContentAlignment.MiddleCenter;
            btn.Cursor = Cursors.Hand;
            return btn;
        }

        // ================= MENÚ DESPLEGABLE SUPERIOR =================
        private void ConstruirMenuSuperior()
        {
            this.menuPrincipal = new MenuStrip();
            this.menuPrincipal.BackColor = Color.White;
            this.menuPrincipal.ForeColor = ColorTranslator.FromHtml("#0f172a");
            this.menuPrincipal.Font = new Font("Segoe UI", 9.5f, FontStyle.Regular);
            this.menuPrincipal.Padding = new Padding(8, 4, 0, 4);

            ToolStripMenuItem menuArchivo = new ToolStripMenuItem("Archivo");

            this.itemIrInicio = new ToolStripMenuItem("Ir al Panel de Inicio");
            this.itemIrInicio.Visible = false;
            this.itemIrInicio.Click += (s, e) =>
            {
                if (this.sesion_iniciada)
                {
                    this.mostrar_dashboard();
                }
            };

            this.itemCerrarSesion = new ToolStripMenuItem("Cerrar Sesión");
            this.itemCerrarSesion.Visible = false;
            this.itemCerrarSesion.Click += (s, e) =>
            {
                if (this.sesion_iniciada)
                {
                    DialogResult r = MessageBox.Show("¿Desea cerrar la sesión actual?", "Confirmar", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                    if (r == DialogResult.Yes)
                    {
                        this.mostrar_login();
                    }
                }
            };

            this.separadorArchivo = new ToolStripSeparator();
            this.separadorArchivo.Visible = false;

            ToolStripMenuItem itemSalir = new ToolStripMenuItem("Salir del Sistema");
            itemSalir.ShortcutKeys = Keys.Alt | Keys.F4;
            itemSalir.Click += (s, e) =>
            {
                DialogResult r = MessageBox.Show("¿Está seguro de que desea salir del sistema SIGEP?", "Salir del Sistema", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (r == DialogResult.Yes)
                {
                    Application.Exit();
                }
            };

            menuArchivo.DropDownItems.Add(this.itemIrInicio);
            menuArchivo.DropDownItems.Add(this.itemCerrarSesion);
            menuArchivo.DropDownItems.Add(this.separadorArchivo);
            menuArchivo.DropDownItems.Add(itemSalir);

            ToolStripMenuItem menuAyuda = new ToolStripMenuItem("Ayuda");
            ToolStripMenuItem itemAcercaDe = new ToolStripMenuItem("Acerca de SIGEP");
            itemAcercaDe.Click += (s, e) => this.MostrarVentanaAcercaDe();
            menuAyuda.DropDownItems.Add(itemAcercaDe);

            this.menuPrincipal.Items.Add(menuArchivo);
            this.menuPrincipal.Items.Add(menuAyuda);
        }

        private void MostrarVentanaAcercaDe()
        {
            Form ventanaAcerca = new Form();
            ventanaAcerca.Text = "Acerca de SIGEP";
            ventanaAcerca.Size = new Size(525, 390);
            ventanaAcerca.FormBorderStyle = FormBorderStyle.FixedDialog;
            ventanaAcerca.StartPosition = FormStartPosition.CenterParent;
            ventanaAcerca.MaximizeBox = false;
            ventanaAcerca.MinimizeBox = false;
            ventanaAcerca.BackColor = ColorTranslator.FromHtml("#f0f0f0");
            ventanaAcerca.Font = new Font("Segoe UI", 9f, FontStyle.Regular);

            PictureBox pbBanner = new PictureBox();
            pbBanner.Location = new Point(18, 16);
            pbBanner.Size = new Size(310, 86);
            pbBanner.BorderStyle = BorderStyle.None;
            pbBanner.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                e.Graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;

                using (System.Drawing.Drawing2D.LinearGradientBrush brush = new System.Drawing.Drawing2D.LinearGradientBrush(
                    pbBanner.ClientRectangle,
                    ColorTranslator.FromHtml("#0f172a"),
                    ColorTranslator.FromHtml("#2563eb"),
                    15f))
                {
                    e.Graphics.FillRectangle(brush, pbBanner.ClientRectangle);
                }

                using (SolidBrush fondoLogo = new SolidBrush(Color.White))
                {
                    e.Graphics.FillRectangle(fondoLogo, 224, 0, 86, 86);
                }

                using (Font fBanner = new Font("Segoe UI", 22f, FontStyle.Bold | FontStyle.Italic))
                using (SolidBrush textoBrush = new SolidBrush(Color.White))
                {
                    e.Graphics.DrawString("SIGEP", fBanner, textoBrush, new PointF(14, 15));
                }

                using (Font fSub = new Font("Segoe UI", 8.5f, FontStyle.Regular))
                using (SolidBrush subBrush = new SolidBrush(ColorTranslator.FromHtml("#cbd5e1")))
                {
                    e.Graphics.DrawString("Sistema de Gestión Parroquial", fSub, subBrush, new PointF(16, 56));
                }

                string ruta_logo = Path.Combine(this.ruta_assets, "logo.png");
                if (File.Exists(ruta_logo))
                {
                    using (Image imgS = Image.FromFile(ruta_logo))
                    {
                        e.Graphics.DrawImage(imgS, new Rectangle(231, 7, 72, 72));
                    }
                }
            };
            ventanaAcerca.Controls.Add(pbBanner);

            PictureBox pbIconoParroquia = new PictureBox();
            pbIconoParroquia.Location = new Point(20, 118);
            pbIconoParroquia.Size = new Size(68, 68);
            pbIconoParroquia.SizeMode = PictureBoxSizeMode.Zoom;
            string ruta_parr = Path.Combine(this.ruta_assets, "parroquia.png");
            if (File.Exists(ruta_parr))
            {
                pbIconoParroquia.Image = Image.FromFile(ruta_parr);
            }
            ventanaAcerca.Controls.Add(pbIconoParroquia);

            Label lblVersion = new Label();
            lblVersion.Text = "SIGEP 1.0 (64-bit)";
            lblVersion.Location = new Point(98, 118);
            lblVersion.AutoSize = true;
            ventanaAcerca.Controls.Add(lblVersion);

            Label lblCopy = new Label();
            lblCopy.Text = "Copyright © 2026 por\nEquipo de Desarrollo SIGEP";
            lblCopy.Location = new Point(98, 142);
            lblCopy.Size = new Size(230, 38);
            ventanaAcerca.Controls.Add(lblCopy);

            Label lblPublicado = new Label();
            lblPublicado.Text = "Publicado para Parroquia San José";
            lblPublicado.Location = new Point(98, 184);
            lblPublicado.AutoSize = true;
            ventanaAcerca.Controls.Add(lblPublicado);

            Label lblSede = new Label();
            lblSede.Text = "Sede Institucional:\nBellavista, Diócesis del Callao";
            lblSede.Location = new Point(98, 218);
            lblSede.Size = new Size(230, 38);
            ventanaAcerca.Controls.Add(lblSede);

            Panel linea1 = new Panel();
            linea1.Location = new Point(98, 270);
            linea1.Size = new Size(230, 1);
            linea1.BackColor = ColorTranslator.FromHtml("#b4b4b4");
            ventanaAcerca.Controls.Add(linea1);

            Label lblLicenciaEstado = new Label();
            lblLicenciaEstado.Text = "Licencia de uso institucional activa";
            lblLicenciaEstado.Location = new Point(98, 280);
            lblLicenciaEstado.AutoSize = true;
            ventanaAcerca.Controls.Add(lblLicenciaEstado);

            Panel linea2 = new Panel();
            linea2.Location = new Point(98, 326);
            linea2.Size = new Size(230, 1);
            linea2.BackColor = ColorTranslator.FromHtml("#b4b4b4");
            ventanaAcerca.Controls.Add(linea2);

            Button btnAceptar = new Button();
            btnAceptar.Text = "Aceptar";
            btnAceptar.Location = new Point(352, 16);
            btnAceptar.Size = new Size(140, 28);
            btnAceptar.BackColor = Color.White;
            btnAceptar.ForeColor = Color.Black;
            btnAceptar.FlatStyle = FlatStyle.Standard;
            btnAceptar.Click += (s, e) => ventanaAcerca.Close();
            ventanaAcerca.Controls.Add(btnAceptar);

            Button btnLicencia = new Button();
            btnLicencia.Text = "Licencia";
            btnLicencia.Location = new Point(352, 52);
            btnLicencia.Size = new Size(140, 28);
            btnLicencia.BackColor = Color.White;
            btnLicencia.ForeColor = Color.Black;
            btnLicencia.FlatStyle = FlatStyle.Standard;
            btnLicencia.Click += (s, e) =>
            {
                MessageBox.Show(
                    "LICENCIA DE USO INSTITUCIONAL\n\n" +
                    "Este software ha sido desarrollado para la gestión documentaria y archivo sacramental de la Parroquia San José de Bellavista, en cumplimiento de los estándares de modernización y notificación electrónica.\n\n" +
                    "Queda prohibida la alteración no autorizada de los registros e índices almacenados.",
                    "Licencia de SIGEP",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            };
            ventanaAcerca.Controls.Add(btnLicencia);

            Button btnContacto = new Button();
            btnContacto.Text = "Contacto";
            btnContacto.Location = new Point(352, 88);
            btnContacto.Size = new Size(140, 28);
            btnContacto.BackColor = Color.White;
            btnContacto.ForeColor = Color.Black;
            btnContacto.FlatStyle = FlatStyle.Standard;
            btnContacto.Click += (s, e) =>
            {
                MessageBox.Show(
                    "INFORMACIÓN DE LA PARROQUIA\n\n" +
                    "• Institución: Parroquia San José de Bellavista\n" +
                    "• Jurisdicción: Diócesis del Callao\n" +
                    "• Dirección: Jirón Bolognesi No. 440 - Bellavista, Callao\n" +
                    "• Teléfono / Celular: 933 352 152\n" +
                    "• Correo: psanjosedebellavista@gmail.com",
                    "Contacto Parroquial",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            };
            ventanaAcerca.Controls.Add(btnContacto);

            ventanaAcerca.AcceptButton = btnAceptar;
            ventanaAcerca.ShowDialog(this);
        }

        private void limpiar_contenedor()
        {
            this.container.Controls.Clear();
        }

        private void limpiar_contenido()
        {
            this.content_frame.Controls.Clear();
        }

        // ================= MOTOR DE BORDES DE COLORES Y VALIDACIÓN =================
        private void ActualizarBorde(Control ctrl, Color color)
        {
            this.coloresBorde[ctrl] = color;
            if (ctrl.Parent != null)
            {
                if (!this.panelesConPaint.Contains(ctrl.Parent))
                {
                    this.panelesConPaint.Add(ctrl.Parent);
                    ctrl.Parent.Paint += PintarBordesControles;
                }
                ctrl.Parent.Invalidate(new Rectangle(ctrl.Left - 4, ctrl.Top - 4, ctrl.Width + 8, ctrl.Height + 8));
            }
        }

        private void PintarBordesControles(object sender, PaintEventArgs e)
        {
            Control panel = sender as Control;
            if (panel == null)
            {
                return;
            }

            foreach (Control c in panel.Controls)
            {
                if (c.Visible && this.coloresBorde.ContainsKey(c))
                {
                    Color col = this.coloresBorde[c];
                    using (Pen pen = new Pen(col, 2f))
                    {
                        e.Graphics.DrawRectangle(pen, c.Left - 1, c.Top - 1, c.Width + 1, c.Height + 1);
                    }
                }
            }
        }

        private bool ignorarEvento = false;

        private void adjuntar_validacion(TextBox entry_widget, string tipo)
        {
            entry_widget.BorderStyle = BorderStyle.FixedSingle;

            entry_widget.VisibleChanged += (s, e) =>
            {
                if (entry_widget.Parent != null)
                {
                    entry_widget.Parent.Invalidate();
                }
            };

            entry_widget.HandleCreated += (s, e) =>
            {
                if (!this.coloresBorde.ContainsKey(entry_widget))
                {
                    this.ActualizarBorde(entry_widget, ColorTranslator.FromHtml("#cbd5e1"));
                }
            };

            if (tipo == "dni")
            {
                entry_widget.MaxLength = 8;
                entry_widget.KeyPress += (s, e) =>
                {
                    if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar))
                    {
                        e.Handled = true;
                    }
                };
            }
            else if (tipo == "texto")
            {
                entry_widget.MaxLength = 80;
                entry_widget.KeyPress += (s, e) =>
                {
                    if (!char.IsControl(e.KeyChar) && !char.IsLetter(e.KeyChar) && e.KeyChar != ' ' && e.KeyChar != '.' && e.KeyChar != '-')
                    {
                        e.Handled = true;
                    }
                };
            }
            else if (tipo == "numero")
            {
                entry_widget.MaxLength = 8;
                entry_widget.KeyPress += (s, e) =>
                {
                    if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar) && e.KeyChar != '.')
                    {
                        e.Handled = true;
                    }
                    if (e.KeyChar == '.' && entry_widget.Text.Contains("."))
                    {
                        e.Handled = true;
                    }
                };
            }
            else if (tipo == "fecha")
            {
                entry_widget.MaxLength = 10;
                entry_widget.KeyPress += (s, e) =>
                {
                    if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar) && e.KeyChar != '/')
                    {
                        e.Handled = true;
                    }
                };
            }
            else if (tipo == "registro_libro")
            {
                // Solo admite números enteros positivos (mayores a 0)
                entry_widget.MaxLength = 6;
                entry_widget.KeyPress += (s, e) =>
                {
                    if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar))
                    {
                        e.Handled = true;
                        return;
                    }
                    if (e.KeyChar == '0' && entry_widget.Text.Length == 0)
                    {
                        e.Handled = true;
                    }
                };
            }

            entry_widget.TextChanged += (sender, e) =>
            {
                if (ignorarEvento)
                {
                    return;
                }
                ignorarEvento = true;
                string val = entry_widget.Text;

                Color verde = ColorTranslator.FromHtml("#10b981");
                Color rojo = ColorTranslator.FromHtml("#ef4444");
                Color gris = ColorTranslator.FromHtml("#cbd5e1");

                if (tipo == "dni")
                {
                    string clean_val = Regex.Replace(val, @"[^0-9]", "");
                    if (clean_val.Length > 8)
                    {
                        clean_val = clean_val.Substring(0, 8);
                    }
                    if (clean_val != val)
                    {
                        entry_widget.Text = clean_val;
                        entry_widget.SelectionStart = clean_val.Length;
                        val = clean_val;
                    }
                    if (val.Length == 8 && val != "00000000")
                    {
                        this.ActualizarBorde(entry_widget, verde);
                    }
                    else
                    {
                        this.ActualizarBorde(entry_widget, rojo);
                    }
                }
                else if (tipo == "texto")
                {
                    string clean_val = Regex.Replace(val, @"[^A-Za-záéíóúÁÉÍÓÚñÑ\s\.\-]", "");
                    if (clean_val != val)
                    {
                        entry_widget.Text = clean_val;
                        entry_widget.SelectionStart = clean_val.Length;
                        val = clean_val;
                    }
                    string soloLetras = Regex.Replace(val, @"[^A-Za-záéíóúÁÉÍÓÚñÑ]", "");
                    if (soloLetras.Length >= 2)
                    {
                        this.ActualizarBorde(entry_widget, verde);
                    }
                    else
                    {
                        this.ActualizarBorde(entry_widget, rojo);
                    }
                }
                else if (tipo == "correo")
                {
                    string clean_val = val.Replace(" ", "");
                    if (clean_val != val)
                    {
                        entry_widget.Text = clean_val;
                        entry_widget.SelectionStart = clean_val.Length;
                        val = clean_val;
                    }
                    if (val == "")
                    {
                        this.ActualizarBorde(entry_widget, gris);
                    }
                    else
                    {
                        string[] lista = val.Split(',');
                        bool todosValidos = true;
                        for (int i = 0; i < lista.Length; i++)
                        {
                            string cLimpio = lista[i].Trim();
                            if (cLimpio == "" || !this.logica.validar_correo_regex(cLimpio))
                            {
                                todosValidos = false;
                            }
                        }
                        if (todosValidos)
                        {
                            this.ActualizarBorde(entry_widget, verde);
                        }
                        else
                        {
                            this.ActualizarBorde(entry_widget, rojo);
                        }
                    }
                }
                else if (tipo == "numero")
                {
                    string clean_val = Regex.Replace(val, @"[^0-9\.]", "");
                    int primerPunto = clean_val.IndexOf('.');
                    if (primerPunto != -1)
                    {
                        string antes = clean_val.Substring(0, primerPunto + 1);
                        string despues = clean_val.Substring(primerPunto + 1).Replace(".", "");
                        clean_val = antes + despues;
                    }
                    if (clean_val != val)
                    {
                        entry_widget.Text = clean_val;
                        entry_widget.SelectionStart = clean_val.Length;
                        val = clean_val;
                    }
                    double monto = 0;
                    if (val.Trim() != "" && double.TryParse(val, NumberStyles.Any, CultureInfo.InvariantCulture, out monto) && monto > 0)
                    {
                        this.ActualizarBorde(entry_widget, verde);
                    }
                    else
                    {
                        this.ActualizarBorde(entry_widget, rojo);
                    }
                }
                else if (tipo == "libre")
                {
                    if (val.Trim().Length >= 1)
                    {
                        this.ActualizarBorde(entry_widget, verde);
                    }
                    else
                    {
                        this.ActualizarBorde(entry_widget, rojo);
                    }
                }
                else if (tipo == "registro_libro")
                {
                    string clean_val = Regex.Replace(val, @"[^0-9]", "").TrimStart('0');
                    if (clean_val != val)
                    {
                        entry_widget.Text = clean_val;
                        entry_widget.SelectionStart = clean_val.Length;
                        val = clean_val;
                    }
                    int numPositivo = 0;
                    if (val == "")
                    {
                        this.ActualizarBorde(entry_widget, gris);
                    }
                    else if (int.TryParse(val, out numPositivo) && numPositivo > 0)
                    {
                        this.ActualizarBorde(entry_widget, verde);
                    }
                    else
                    {
                        this.ActualizarBorde(entry_widget, rojo);
                    }
                }
                else if (tipo == "fecha")
                {
                    string clean_val = Regex.Replace(val, @"[^0-9/]", "");
                    if (clean_val.Length > 10)
                    {
                        clean_val = clean_val.Substring(0, 10);
                    }
                    if (clean_val != val)
                    {
                        entry_widget.Text = clean_val;
                        entry_widget.SelectionStart = clean_val.Length;
                        val = clean_val;
                    }

                    bool es_valido = true;
                    if (val != "")
                    {
                        string[] partes = val.Split('/');
                        if (partes.Length > 3)
                        {
                            es_valido = false;
                        }
                        if (partes.Length >= 1 && partes[0].Length > 0)
                        {
                            if (partes[0].Length > 2)
                            {
                                es_valido = false;
                            }
                            else
                            {
                                int dia = 0;
                                if (int.TryParse(partes[0], out dia))
                                {
                                    if (dia > 31 || dia == 0)
                                    {
                                        es_valido = false;
                                    }
                                }
                            }
                        }
                        if (partes.Length >= 2 && partes[1].Length > 0)
                        {
                            if (partes[1].Length > 2)
                            {
                                es_valido = false;
                            }
                            else
                            {
                                int mes = 0;
                                if (int.TryParse(partes[1], out mes))
                                {
                                    if (mes > 12 || mes == 0)
                                    {
                                        es_valido = false;
                                    }
                                }
                            }
                        }
                        if (partes.Length >= 3 && partes[2].Length > 4)
                        {
                            es_valido = false;
                        }
                        if (val.Length == 10 && es_valido)
                        {
                            DateTime dt;
                            if (!DateTime.TryParseExact(val, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out dt))
                            {
                                es_valido = false;
                            }
                        }
                    }

                    if (val == "")
                    {
                        this.ActualizarBorde(entry_widget, gris);
                    }
                    else if (es_valido)
                    {
                        this.ActualizarBorde(entry_widget, verde);
                    }
                    else
                    {
                        this.ActualizarBorde(entry_widget, rojo);
                    }
                }

                ignorarEvento = false;
            };
        }

        private void VincularAutocompletadoReniec(TextBox txtDniActual, TextBox txtNomActual, TextBox txtOtroDni)
        {
            string ultimoDniConsultado = "";
            txtDniActual.TextChanged += (s, e) =>
            {
                string val = txtDniActual.Text.Trim();
                if (val.Length < 8)
                {
                    ultimoDniConsultado = "";
                    return;
                }
                if (val.Length == 8 && val != ultimoDniConsultado)
                {
                    if (val == "00000000")
                    {
                        ultimoDniConsultado = val;
                        this.ActualizarBorde(txtDniActual, ColorTranslator.FromHtml("#ef4444"));
                        MessageBox.Show("El número de DNI no puede estar compuesto únicamente por ceros.", "DNI Inválido", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    if (txtOtroDni != null && txtOtroDni.Visible && txtOtroDni.Text.Trim() == val)
                    {
                        ultimoDniConsultado = val;
                        this.ActualizarBorde(txtDniActual, ColorTranslator.FromHtml("#ef4444"));
                        txtNomActual.Text = "";
                        MessageBox.Show("No puede ingresar dos números de DNI iguales en el mismo registro. Deben ser diferentes.", "DNI Duplicado", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    ultimoDniConsultado = val;
                    Cursor.Current = Cursors.WaitCursor;
                    string res = ApiReniec.consultar_dni(val);
                    Cursor.Current = Cursors.Default;
                    if (res != "")
                    {
                        txtNomActual.Text = res;
                    }
                    else
                    {
                        MessageBox.Show("DNI no encontrado en RENIEC. Ingrese manualmente el nombre.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
            };
        }

        private void VincularAutocompletadoReniecSeparado(TextBox txtDniActual, TextBox txtNombre, TextBox txtApellido, TextBox txtOtroDni, Func<bool> estaCargandoTabla)
        {
            string ultimoDniConsultado = "";
            txtDniActual.TextChanged += (s, e) =>
            {
                if (estaCargandoTabla != null && estaCargandoTabla())
                {
                    ultimoDniConsultado = txtDniActual.Text.Trim();
                    return;
                }

                string val = txtDniActual.Text.Trim();
                if (val.Length < 8)
                {
                    ultimoDniConsultado = "";
                    return;
                }
                if (val.Length == 8 && val != ultimoDniConsultado)
                {
                    if (val == "00000000")
                    {
                        ultimoDniConsultado = val;
                        this.ActualizarBorde(txtDniActual, ColorTranslator.FromHtml("#ef4444"));
                        MessageBox.Show("El número de DNI no puede estar compuesto únicamente por ceros.", "DNI Inválido", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    if (txtOtroDni != null && txtOtroDni.Visible && txtOtroDni.Text.Trim() == val)
                    {
                        ultimoDniConsultado = val;
                        this.ActualizarBorde(txtDniActual, ColorTranslator.FromHtml("#ef4444"));
                        txtNombre.Text = "";
                        txtApellido.Text = "";
                        MessageBox.Show("No puede colocar 2 DNIs iguales. Los números de DNI deben ser diferentes.", "DNI Duplicado", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    ultimoDniConsultado = val;
                    Cursor.Current = Cursors.WaitCursor;
                    string nom;
                    string ape;
                    bool ok = ApiReniec.consultar_dni_separado(val, out nom, out ape);
                    Cursor.Current = Cursors.Default;

                    if (ok)
                    {
                        txtNombre.Text = nom;
                        txtApellido.Text = ape;
                    }
                    else
                    {
                        MessageBox.Show("DNI no encontrado en RENIEC. Ingrese manualmente el nombre y apellido.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
            };
        }

        private bool EsNombreValido(string nombre)
        {
            if (string.IsNullOrWhiteSpace(nombre))
            {
                return false;
            }
            string soloLetras = Regex.Replace(nombre, @"[^A-Za-záéíóúÁÉÍÓÚñÑ]", "");
            if (soloLetras.Length < 2)
            {
                return false;
            }
            return true;
        }

        private bool EsNumeroPositivoValido(string texto)
        {
            if (string.IsNullOrWhiteSpace(texto))
            {
                return false;
            }
            int num = 0;
            if (int.TryParse(texto.Trim(), out num) && num > 0)
            {
                return true;
            }
            return false;
        }

        // ================= 1. LOGIN DIRECTO =================
        private void mostrar_login()
        {
            this.sesion_iniciada = false;
            this.itemIrInicio.Visible = false;
            this.itemCerrarSesion.Visible = false;
            this.separadorArchivo.Visible = false;
            this.limpiar_contenedor();

            Panel f_login = new Panel();
            f_login.Size = new Size(850, 550);
            f_login.BackColor = Color.White;
            f_login.BorderStyle = BorderStyle.FixedSingle;

            Panel f_izq = new Panel();
            f_izq.Size = new Size(350, 550);
            f_izq.Dock = DockStyle.Left;
            f_izq.BackColor = ColorTranslator.FromHtml("#1e293b");

            string ruta_imglogin = Path.Combine(this.ruta_assets, "imglogin.png");
            if (File.Exists(ruta_imglogin))
            {
                PictureBox pb = new PictureBox();
                pb.Dock = DockStyle.Fill;
                pb.Image = Image.FromFile(ruta_imglogin);
                pb.SizeMode = PictureBoxSizeMode.StretchImage;
                f_izq.Controls.Add(pb);
            }
            else
            {
                Label lbl_err = new Label();
                lbl_err.Text = "Parroquia San José de Bellavista";
                lbl_err.ForeColor = Color.White;
                lbl_err.Dock = DockStyle.Fill;
                lbl_err.TextAlign = ContentAlignment.MiddleCenter;
                f_izq.Controls.Add(lbl_err);
            }

            Panel f_der = new Panel();
            f_der.Dock = DockStyle.Fill;
            f_der.BackColor = Color.White;

            PictureBox pbSigep = new PictureBox();
            pbSigep.Size = new Size(110, 110);
            pbSigep.Location = new Point(130, 40);
            pbSigep.SizeMode = PictureBoxSizeMode.Zoom;
            string ruta_logo_sigep = Path.Combine(this.ruta_assets, "logo.png");
            if (File.Exists(ruta_logo_sigep))
            {
                pbSigep.Image = Image.FromFile(ruta_logo_sigep);
            }
            f_der.Controls.Add(pbSigep);

            PictureBox pbParr = new PictureBox();
            pbParr.Size = new Size(110, 110);
            pbParr.Location = new Point(260, 40);
            pbParr.SizeMode = PictureBoxSizeMode.Zoom;
            string ruta_logo_parroquia = Path.Combine(this.ruta_assets, "parroquia.png");
            if (File.Exists(ruta_logo_parroquia))
            {
                pbParr.Image = Image.FromFile(ruta_logo_parroquia);
            }
            f_der.Controls.Add(pbParr);

            Label lblBienv = new Label();
            lblBienv.Text = "Bienvenido a SIGEP";
            lblBienv.Font = new Font("Segoe UI", 22f, FontStyle.Bold);
            lblBienv.ForeColor = ColorTranslator.FromHtml("#0f172a");
            lblBienv.Size = new Size(500, 45);
            lblBienv.Location = new Point(0, 165);
            lblBienv.TextAlign = ContentAlignment.MiddleCenter;
            f_der.Controls.Add(lblBienv);

            Label lblSub = new Label();
            lblSub.Text = "Inicie sesión para continuar";
            lblSub.Font = new Font("Segoe UI", 12f, FontStyle.Regular);
            lblSub.ForeColor = ColorTranslator.FromHtml("#64748b");
            lblSub.Size = new Size(500, 30);
            lblSub.Location = new Point(0, 210);
            lblSub.TextAlign = ContentAlignment.MiddleCenter;
            f_der.Controls.Add(lblSub);

            Label lblU = new Label();
            lblU.Text = "Usuario:";
            lblU.Font = new Font("Segoe UI", 10f, FontStyle.Bold);
            lblU.Location = new Point(90, 260);
            lblU.AutoSize = true;
            f_der.Controls.Add(lblU);

            TextBox txt_u = new TextBox();
            txt_u.Font = new Font("Segoe UI", 12f);
            txt_u.Size = new Size(320, 35);
            txt_u.Location = new Point(90, 285);
            f_der.Controls.Add(txt_u);

            Label lblP = new Label();
            lblP.Text = "Contraseña:";
            lblP.Font = new Font("Segoe UI", 10f, FontStyle.Bold);
            lblP.Location = new Point(90, 335);
            lblP.AutoSize = true;
            f_der.Controls.Add(lblP);

            TextBox txt_p = new TextBox();
            txt_p.Font = new Font("Segoe UI", 12f);
            txt_p.Size = new Size(320, 35);
            txt_p.Location = new Point(90, 360);
            txt_p.PasswordChar = '*';
            f_der.Controls.Add(txt_p);

            this.adjuntar_validacion(txt_u, "libre");
            this.adjuntar_validacion(txt_p, "libre");

            Button btn_ingresar = CrearBotonAccion("Ingresar al sistema", ColorTranslator.FromHtml("#2563eb"), new Point(90, 420), new Size(320, 45), 12f);
            f_der.Controls.Add(btn_ingresar);

            Action intentar_login = () =>
            {
                if (txt_u.Text.Trim().Length == 0 || txt_p.Text.Trim().Length == 0)
                {
                    MessageBox.Show("Por favor, ingrese su usuario y contraseña.", "Campos Requeridos", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                if (this.logica.validar_credenciales(txt_u.Text.Trim(), txt_p.Text.Trim()))
                {
                    this.intentos_login = 0;
                    this.sesion_iniciada = true;
                    this.itemIrInicio.Visible = true;
                    this.itemCerrarSesion.Visible = true;
                    this.separadorArchivo.Visible = true;
                    this.construir_interfaz_principal();
                }
                else
                {
                    this.intentos_login += 1;
                    if (this.intentos_login >= 3)
                    {
                        MessageBox.Show("Límite de intentos superado. El sistema se cerrará.", "Seguridad", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        Application.Exit();
                    }
                    else
                    {
                        int intentos_rest = 3 - this.intentos_login;
                        MessageBox.Show("Credenciales incorrectas. Quedan " + intentos_rest + " intentos.", "Acceso Denegado", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            };

            btn_ingresar.Click += (s, e) => intentar_login();
            txt_p.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    e.SuppressKeyPress = true;
                    intentar_login();
                }
            };

            f_login.Controls.Add(f_der);
            f_login.Controls.Add(f_izq);
            this.container.Controls.Add(f_login);

            Action centrar = () =>
            {
                f_login.Location = new Point(
                    Math.Max(0, (this.container.Width - f_login.Width) / 2),
                    Math.Max(0, (this.container.Height - f_login.Height) / 2)
                );
            };
            centrar();
            this.container.Resize += (s, e) => centrar();
        }

        // ================= 2. SIDEBAR Y CONTENEDOR =================
        private void construir_interfaz_principal()
        {
            this.limpiar_contenedor();

            this.sidebar = new Panel();
            this.sidebar.Width = 250;
            this.sidebar.Dock = DockStyle.Left;
            this.sidebar.BackColor = ColorTranslator.FromHtml("#1e293b");

            string ruta_logo_blanco = Path.Combine(this.ruta_assets, "logoblanco.png");
            if (File.Exists(ruta_logo_blanco))
            {
                PictureBox pbLogo = new PictureBox();
                pbLogo.Size = new Size(85, 85);
                pbLogo.Location = new Point(82, 30);
                pbLogo.SizeMode = PictureBoxSizeMode.Zoom;
                pbLogo.Image = Image.FromFile(ruta_logo_blanco);
                this.sidebar.Controls.Add(pbLogo);
            }

            Button btnDash = CrearBotonSidebar("📊 Panel de Inicio", 150);
            btnDash.Click += (s, e) => this.mostrar_dashboard();
            this.sidebar.Controls.Add(btnDash);

            Button btnReg = CrearBotonSidebar("📝 Nuevo Expediente", 200);
            btnReg.Click += (s, e) => this.mostrar_registro();
            this.sidebar.Controls.Add(btnReg);

            Button btnBus = CrearBotonSidebar("🔍 Buscar y Editar", 250);
            btnBus.Click += (s, e) => this.mostrar_buscar();
            this.sidebar.Controls.Add(btnBus);

            Button btnRep = CrearBotonSidebar("📑 Reporte de Expedientes", 300);
            btnRep.Click += (s, e) => this.mostrar_reportes();
            this.sidebar.Controls.Add(btnRep);

            Button btnConf = CrearBotonSidebar("⚙ Configuración de Fechas", 350);
            btnConf.Click += (s, e) => this.mostrar_configuracion();
            this.sidebar.Controls.Add(btnConf);

            Button btnAvanzadas = CrearBotonSidebar("📜 Opciones Avanzadas", 400);
            btnAvanzadas.Click += (s, e) => this.mostrar_opciones_avanzadas();
            this.sidebar.Controls.Add(btnAvanzadas);

            this.content_frame = new Panel();
            this.content_frame.Dock = DockStyle.Fill;
            this.content_frame.BackColor = ColorTranslator.FromHtml("#f8fafc");
            this.content_frame.AutoScroll = true;

            this.container.Controls.Add(this.content_frame);
            this.container.Controls.Add(this.sidebar);

            this.mostrar_dashboard();
        }

        private Button CrearBotonSidebar(string texto, int top)
        {
            Button btn = new Button();
            btn.Text = "   " + texto;
            btn.Font = new Font("Segoe UI", 11f, FontStyle.Regular);
            btn.Size = new Size(250, 44);
            btn.Location = new Point(0, top);
            btn.UseVisualStyleBackColor = false;
            btn.FlatStyle = FlatStyle.Flat;
            btn.FlatAppearance.BorderSize = 0;
            btn.FlatAppearance.MouseOverBackColor = ColorTranslator.FromHtml("#334155");
            btn.BackColor = Color.Transparent;
            btn.ForeColor = Color.White;
            btn.TextAlign = ContentAlignment.MiddleLeft;
            btn.Cursor = Cursors.Hand;
            return btn;
        }

        // ================= 3. DASHBOARD =================
        private void mostrar_dashboard()
        {
            this.limpiar_contenido();

            Label lblHola = new Label();
            lblHola.Text = "Hola, Bienvenida";
            lblHola.Font = new Font("Segoe UI", 24f, FontStyle.Bold);
            lblHola.ForeColor = ColorTranslator.FromHtml("#0f172a");
            lblHola.Location = new Point(40, 40);
            lblHola.AutoSize = true;
            this.content_frame.Controls.Add(lblHola);

            Label lblSub = new Label();
            lblSub.Text = "Resumen de Operaciones - Parroquia San José";
            lblSub.Font = new Font("Segoe UI", 12f, FontStyle.Regular);
            lblSub.ForeColor = ColorTranslator.FromHtml("#64748b");
            lblSub.Location = new Point(40, 85);
            lblSub.AutoSize = true;
            this.content_frame.Controls.Add(lblSub);

            Panel card1 = new Panel();
            card1.Size = new Size(350, 120);
            card1.Location = new Point(40, 130);
            card1.BackColor = Color.White;
            card1.BorderStyle = BorderStyle.FixedSingle;

            Label lblCardTit = new Label();
            lblCardTit.Text = "EXPEDIENTES TOTALES";
            lblCardTit.Font = new Font("Segoe UI", 10f, FontStyle.Bold);
            lblCardTit.ForeColor = ColorTranslator.FromHtml("#10b981");
            lblCardTit.Location = new Point(20, 20);
            lblCardTit.AutoSize = true;
            card1.Controls.Add(lblCardTit);

            Label lblTotal = new Label();
            lblTotal.Text = this.db.nombres.Count.ToString();
            lblTotal.Font = new Font("Segoe UI", 26f, FontStyle.Bold);
            lblTotal.ForeColor = ColorTranslator.FromHtml("#0f172a");
            lblTotal.Location = new Point(20, 50);
            lblTotal.AutoSize = true;
            card1.Controls.Add(lblTotal);

            this.content_frame.Controls.Add(card1);
        }

        // ================= 4. CONFIGURACIÓN =================
        private void mostrar_configuracion()
        {
            this.limpiar_contenido();

            Label lblTit = new Label();
            lblTit.Text = "Configuración de Fechas de Catequesis";
            lblTit.Font = new Font("Segoe UI", 20f, FontStyle.Bold);
            lblTit.ForeColor = ColorTranslator.FromHtml("#2563eb");
            lblTit.Location = new Point(40, 30);
            lblTit.AutoSize = true;
            this.content_frame.Controls.Add(lblTit);

            Panel f_conf = new Panel();
            f_conf.Size = new Size(650, 250);
            f_conf.Location = new Point(40, 80);
            f_conf.BackColor = Color.White;
            f_conf.BorderStyle = BorderStyle.FixedSingle;

            Label lblSub = new Label();
            lblSub.Text = "Selecciona la fecha de inicio (Día / Mes)";
            lblSub.Font = new Font("Segoe UI", 12f, FontStyle.Bold);
            lblSub.Location = new Point(20, 20);
            lblSub.AutoSize = true;
            f_conf.Controls.Add(lblSub);

            List<string> lista_dias = new List<string>();
            lista_dias.Add("--");
            for (int i = 1; i <= 31; i++)
            {
                lista_dias.Add(i.ToString("D2"));
            }

            List<string> lista_meses = new List<string>();
            lista_meses.Add("--");
            for (int i = 1; i <= 12; i++)
            {
                lista_meses.Add(i.ToString("D2"));
            }

            string[] partes_com = this.config.fecha_comunion.Split('/');
            string c_dia = "--";
            string c_mes = "--";
            if (partes_com.Length == 2)
            {
                c_dia = partes_com[0];
                c_mes = partes_com[1];
            }

            Label lblCom = new Label();
            lblCom.Text = "Primera Comunión:";
            lblCom.Location = new Point(20, 70);
            lblCom.Size = new Size(180, 25);
            f_conf.Controls.Add(lblCom);

            ComboBox cb_d_com = new ComboBox();
            cb_d_com.DropDownStyle = ComboBoxStyle.DropDownList;
            cb_d_com.Items.AddRange(lista_dias.ToArray());
            cb_d_com.SelectedItem = c_dia;
            cb_d_com.Location = new Point(210, 68);
            cb_d_com.Width = 70;
            f_conf.Controls.Add(cb_d_com);

            ComboBox cb_m_com = new ComboBox();
            cb_m_com.DropDownStyle = ComboBoxStyle.DropDownList;
            cb_m_com.Items.AddRange(lista_meses.ToArray());
            cb_m_com.SelectedItem = c_mes;
            cb_m_com.Location = new Point(290, 68);
            cb_m_com.Width = 70;
            f_conf.Controls.Add(cb_m_com);

            string[] partes_con = this.config.fecha_confirmacion.Split('/');
            string cf_dia = "--";
            string cf_mes = "--";
            if (partes_con.Length == 2)
            {
                cf_dia = partes_con[0];
                cf_mes = partes_con[1];
            }

            Label lblCon = new Label();
            lblCon.Text = "Confirmación:";
            lblCon.Location = new Point(20, 115);
            lblCon.Size = new Size(180, 25);
            f_conf.Controls.Add(lblCon);

            ComboBox cb_d_con = new ComboBox();
            cb_d_con.DropDownStyle = ComboBoxStyle.DropDownList;
            cb_d_con.Items.AddRange(lista_dias.ToArray());
            cb_d_con.SelectedItem = cf_dia;
            cb_d_con.Location = new Point(210, 113);
            cb_d_con.Width = 70;
            f_conf.Controls.Add(cb_d_con);

            ComboBox cb_m_con = new ComboBox();
            cb_m_con.DropDownStyle = ComboBoxStyle.DropDownList;
            cb_m_con.Items.AddRange(lista_meses.ToArray());
            cb_m_con.SelectedItem = cf_mes;
            cb_m_con.Location = new Point(290, 113);
            cb_m_con.Width = 70;
            f_conf.Controls.Add(cb_m_con);

            Button btnGuardar = CrearBotonAccion("Guardar Cambios", ColorTranslator.FromHtml("#10b981"), new Point(20, 175), new Size(180, 42));

            btnGuardar.Click += (s, e) =>
            {
                string dia_com = cb_d_com.SelectedItem.ToString();
                string mes_com = cb_m_com.SelectedItem.ToString();
                string dia_con = cb_d_con.SelectedItem.ToString();
                string mes_con = cb_m_con.SelectedItem.ToString();

                DateTime now = DateTime.Now;
                int current_year = now.Year;

                if (dia_com != "--" || mes_com != "--")
                {
                    if (dia_com == "--" || mes_com == "--")
                    {
                        MessageBox.Show("Para Primera Comunión, debe seleccionar día y mes, o dejar ambos en '--'.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                    try
                    {
                        DateTime fecha_com_obj = new DateTime(current_year, int.Parse(mes_com), int.Parse(dia_com));
                        if (fecha_com_obj.Date < now.Date)
                        {
                            MessageBox.Show("La fecha de Primera Comunión configurada es anterior al día de hoy.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return;
                        }
                    }
                    catch
                    {
                        MessageBox.Show("La fecha de Primera Comunión no existe en el calendario (ej. 31 de Febrero).", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                }

                if (dia_con != "--" || mes_con != "--")
                {
                    if (dia_con == "--" || mes_con == "--")
                    {
                        MessageBox.Show("Para Confirmación, debe seleccionar día y mes, o dejar ambos en '--'.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                    try
                    {
                        DateTime fecha_con_obj = new DateTime(current_year, int.Parse(mes_con), int.Parse(dia_con));
                        if (fecha_con_obj.Date < now.Date)
                        {
                            MessageBox.Show("La fecha de Confirmación configurada es anterior al día de hoy.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return;
                        }
                    }
                    catch
                    {
                        MessageBox.Show("La fecha de Confirmación no existe en el calendario (ej. 31 de Febrero).", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                }

                if (dia_com == "--" && dia_con == "--")
                {
                    MessageBox.Show("Debe configurar al menos una de las fechas (Primera Comunión o Confirmación).", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                this.config.fecha_comunion = dia_com + "/" + mes_com;
                this.config.fecha_confirmacion = dia_con + "/" + mes_con;
                this.config.guardar();
                MessageBox.Show("Fechas actualizadas correctamente.", "Éxito", MessageBoxButtons.OK, MessageBoxIcon.Information);
            };

            f_conf.Controls.Add(btnGuardar);
            this.content_frame.Controls.Add(f_conf);
        }

        // ================= 5. REGISTRO COMPLETO =================
        private void mostrar_registro()
        {
            this.limpiar_contenido();

            Label lblTit = new Label();
            lblTit.Text = "Registro Documentario";
            lblTit.Font = new Font("Segoe UI", 20f, FontStyle.Bold);
            lblTit.ForeColor = ColorTranslator.FromHtml("#2563eb");
            lblTit.Location = new Point(40, 15);
            lblTit.AutoSize = true;
            this.content_frame.Controls.Add(lblTit);

            Panel f_main = new Panel();
            f_main.Location = new Point(40, 60);
            f_main.Size = new Size(920, 720);
            f_main.BackColor = Color.White;
            f_main.BorderStyle = BorderStyle.FixedSingle;

            Label lblTram = new Label();
            lblTram.Text = "Trámite:";
            lblTram.Font = new Font("Segoe UI", 10f, FontStyle.Bold);
            lblTram.Location = new Point(30, 20);
            lblTram.AutoSize = true;
            f_main.Controls.Add(lblTram);

            ComboBox cb_tipo = new ComboBox();
            cb_tipo.DropDownStyle = ComboBoxStyle.DropDownList;
            cb_tipo.Items.AddRange(new string[] { "Bautizo", "Primera Comunión", "Confirmación", "Matrimonio Regular" });
            cb_tipo.Location = new Point(160, 17);
            cb_tipo.Width = 200;
            f_main.Controls.Add(cb_tipo);

            Label lblGen = new Label();
            lblGen.Text = "Género Titular:";
            lblGen.Font = new Font("Segoe UI", 10f, FontStyle.Bold);
            lblGen.Location = new Point(400, 20);
            lblGen.AutoSize = true;
            f_main.Controls.Add(lblGen);

            ComboBox cb_gen = new ComboBox();
            cb_gen.DropDownStyle = ComboBoxStyle.DropDownList;
            cb_gen.Items.AddRange(new string[] { "Femenino", "Masculino" });
            cb_gen.SelectedIndex = 0;
            cb_gen.Location = new Point(520, 17);
            cb_gen.Width = 150;
            f_main.Controls.Add(cb_gen);

            Label lbl_d1 = new Label();
            lbl_d1.Text = "DNI Bautizado:";
            lbl_d1.Font = new Font("Segoe UI", 10f, FontStyle.Bold);
            lbl_d1.Location = new Point(30, 60);
            lbl_d1.Size = new Size(125, 25);
            f_main.Controls.Add(lbl_d1);

            TextBox txt_d1 = new TextBox();
            txt_d1.Location = new Point(160, 58);
            txt_d1.Width = 120;
            f_main.Controls.Add(txt_d1);

            TextBox txt_nom1 = new TextBox();
            txt_nom1.Location = new Point(290, 58);
            txt_nom1.Width = 280;
            AsignarPlaceholder(txt_nom1, "Nombre Titular");
            f_main.Controls.Add(txt_nom1);

            this.adjuntar_validacion(txt_d1, "dni");
            this.adjuntar_validacion(txt_nom1, "texto");

            Label lbl_d2 = new Label();
            lbl_d2.Text = "DNI Apoderado:";
            lbl_d2.Font = new Font("Segoe UI", 10f, FontStyle.Bold);
            lbl_d2.Location = new Point(30, 95);
            lbl_d2.Size = new Size(125, 25);
            f_main.Controls.Add(lbl_d2);

            TextBox txt_d2 = new TextBox();
            txt_d2.Location = new Point(160, 93);
            txt_d2.Width = 120;
            f_main.Controls.Add(txt_d2);

            TextBox txt_nom2 = new TextBox();
            txt_nom2.Location = new Point(290, 93);
            txt_nom2.Width = 280;
            AsignarPlaceholder(txt_nom2, "Nombre Apoderado");
            f_main.Controls.Add(txt_nom2);

            this.adjuntar_validacion(txt_d2, "dni");
            this.adjuntar_validacion(txt_nom2, "texto");

            this.VincularAutocompletadoReniec(txt_d1, txt_nom1, txt_d2);
            this.VincularAutocompletadoReniec(txt_d2, txt_nom2, txt_d1);

            Label lblNotif = new Label();
            lblNotif.Text = "Notificar a:";
            lblNotif.Font = new Font("Segoe UI", 10f, FontStyle.Bold);
            lblNotif.Location = new Point(30, 130);
            lblNotif.Size = new Size(125, 25);
            f_main.Controls.Add(lblNotif);

            ComboBox cb_opc_cor = new ComboBox();
            cb_opc_cor.DropDownStyle = ComboBoxStyle.DropDownList;
            cb_opc_cor.Items.AddRange(new string[] { "Solo Novio", "Solo Novia", "Ambos" });
            cb_opc_cor.SelectedIndex = 2;
            cb_opc_cor.Location = new Point(160, 128);
            cb_opc_cor.Width = 150;
            f_main.Controls.Add(cb_opc_cor);

            Label lbl_cor1 = new Label();
            lbl_cor1.Text = "Correo Notif.:";
            lbl_cor1.Font = new Font("Segoe UI", 10f, FontStyle.Bold);
            lbl_cor1.Location = new Point(30, 165);
            lbl_cor1.Size = new Size(125, 25);
            f_main.Controls.Add(lbl_cor1);

            TextBox txt_cor1 = new TextBox();
            txt_cor1.Location = new Point(160, 163);
            txt_cor1.Width = 250;
            f_main.Controls.Add(txt_cor1);

            Label lbl_cor2 = new Label();
            lbl_cor2.Text = "Correo Novia:";
            lbl_cor2.Font = new Font("Segoe UI", 10f, FontStyle.Bold);
            lbl_cor2.Location = new Point(430, 165);
            lbl_cor2.Size = new Size(105, 25);
            f_main.Controls.Add(lbl_cor2);

            TextBox txt_cor2 = new TextBox();
            txt_cor2.Location = new Point(540, 163);
            txt_cor2.Width = 250;
            f_main.Controls.Add(txt_cor2);

            this.adjuntar_validacion(txt_cor1, "correo");
            this.adjuntar_validacion(txt_cor2, "correo");

            Panel f_req = new Panel();
            f_req.Location = new Point(30, 205);
            f_req.Size = new Size(850, 165);
            f_req.BackColor = ColorTranslator.FromHtml("#f8fafc");
            f_req.BorderStyle = BorderStyle.FixedSingle;

            Label lblReqTit = new Label();
            lblReqTit.Text = "Requisitos de Documentación:";
            lblReqTit.Font = new Font("Segoe UI", 10f, FontStyle.Bold);
            lblReqTit.Location = new Point(15, 10);
            lblReqTit.AutoSize = true;
            f_req.Controls.Add(lblReqTit);

            this.chks.Clear();
            for (int i = 0; i < 8; i++)
            {
                CheckBox chk = new CheckBox();
                chk.AutoSize = true;
                int col = i % 2;
                int fila = i / 2;
                chk.Location = new Point(20 + (col * 400), 40 + (fila * 28));
                this.chks.Add(chk);
                f_req.Controls.Add(chk);
            }
            f_main.Controls.Add(f_req);

            Label lblPago = new Label();
            lblPago.Text = "Estado de Pago:";
            lblPago.Font = new Font("Segoe UI", 10f, FontStyle.Bold);
            lblPago.Location = new Point(30, 385);
            lblPago.Size = new Size(125, 25);
            f_main.Controls.Add(lblPago);

            ComboBox cb_pago = new ComboBox();
            cb_pago.DropDownStyle = ComboBoxStyle.DropDownList;
            cb_pago.Items.AddRange(new string[] { "Pagado", "Falta Pagar" });
            cb_pago.SelectedIndex = 0;
            cb_pago.Location = new Point(160, 383);
            cb_pago.Width = 150;
            f_main.Controls.Add(cb_pago);

            TextBox txt_deuda = new TextBox();
            txt_deuda.Location = new Point(330, 383);
            txt_deuda.Width = 150;
            txt_deuda.Visible = false;
            AsignarPlaceholder(txt_deuda, "Monto deuda S/");
            this.adjuntar_validacion(txt_deuda, "numero");
            f_main.Controls.Add(txt_deuda);

            Panel f_obs_izq = new Panel();
            f_obs_izq.Location = new Point(30, 425);
            f_obs_izq.Size = new Size(415, 180);
            f_obs_izq.BackColor = ColorTranslator.FromHtml("#f1f5f9");
            f_obs_izq.BorderStyle = BorderStyle.FixedSingle;

            Label lblObsGen = new Label();
            lblObsGen.Text = "Observación Generada:";
            lblObsGen.Font = new Font("Segoe UI", 10f, FontStyle.Bold);
            lblObsGen.Location = new Point(15, 10);
            lblObsGen.AutoSize = true;
            f_obs_izq.Controls.Add(lblObsGen);

            TextBox txt_obs_auto = new TextBox();
            txt_obs_auto.Multiline = true;
            txt_obs_auto.ReadOnly = true;
            txt_obs_auto.ScrollBars = ScrollBars.Vertical;
            txt_obs_auto.Location = new Point(15, 38);
            txt_obs_auto.Size = new Size(385, 125);
            txt_obs_auto.BackColor = Color.White;
            f_obs_izq.Controls.Add(txt_obs_auto);
            f_main.Controls.Add(f_obs_izq);

            Panel f_obs_der = new Panel();
            f_obs_der.Location = new Point(465, 425);
            f_obs_der.Size = new Size(415, 180);
            f_obs_der.BackColor = ColorTranslator.FromHtml("#f1f5f9");
            f_obs_der.BorderStyle = BorderStyle.FixedSingle;

            CheckBox chk_adic = new CheckBox();
            chk_adic.Text = "¿Añadir observación adicional manual?";
            chk_adic.Font = new Font("Segoe UI", 10f, FontStyle.Bold);
            chk_adic.Location = new Point(15, 10);
            chk_adic.AutoSize = true;
            f_obs_der.Controls.Add(chk_adic);

            TextBox txt_obs_adic = new TextBox();
            txt_obs_adic.Multiline = true;
            txt_obs_adic.ScrollBars = ScrollBars.Vertical;
            txt_obs_adic.Location = new Point(15, 38);
            txt_obs_adic.Size = new Size(385, 125);
            txt_obs_adic.Visible = false;
            f_obs_der.Controls.Add(txt_obs_adic);
            f_main.Controls.Add(f_obs_der);

            Action actualizar_obs_auto = () =>
            {
                List<string> faltantes = new List<string>();
                for (int i = 0; i < this.reqs_activos; i++)
                {
                    if (!this.chks[i].Checked)
                    {
                        string texto_limpio = this.chks[i].Text.Substring(3);
                        faltantes.Add(texto_limpio);
                    }
                }

                string texto = "";
                if (faltantes.Count > 0)
                {
                    texto = "Faltan los siguientes documentos físicos:\r\n- " + string.Join("\r\n- ", faltantes) + "\r\n";
                }
                else
                {
                    texto = "Documentación física completa.\r\n";
                }

                if (cb_pago.SelectedItem != null && cb_pago.SelectedItem.ToString() == "Falta Pagar")
                {
                    texto = texto + "Pendiente de pago por regularizar.";
                }

                txt_obs_auto.Text = texto.Trim();
            };

            for (int i = 0; i < 8; i++)
            {
                this.chks[i].CheckedChanged += (s, e) => actualizar_obs_auto();
            }

            cb_pago.SelectedIndexChanged += (s, e) =>
            {
                if (cb_pago.SelectedItem.ToString() == "Falta Pagar")
                {
                    txt_deuda.Visible = true;
                    this.ActualizarBorde(txt_deuda, ColorTranslator.FromHtml("#ef4444"));
                }
                else
                {
                    txt_deuda.Visible = false;
                    txt_deuda.Text = "";
                }
                actualizar_obs_auto();
            };

            chk_adic.CheckedChanged += (s, e) =>
            {
                if (chk_adic.Checked)
                {
                    txt_obs_adic.Visible = true;
                }
                else
                {
                    txt_obs_adic.Visible = false;
                    txt_obs_adic.Text = "";
                }
            };

            Action<string> cambiar_modo_correo = (opc) =>
            {
                lbl_cor2.Visible = false;
                txt_cor2.Visible = false;

                if (cb_tipo.SelectedItem != null && cb_tipo.SelectedItem.ToString() == "Matrimonio Regular")
                {
                    if (opc == "Solo Novio")
                    {
                        lbl_cor1.Text = "Correo Novio:";
                    }
                    else if (opc == "Solo Novia")
                    {
                        lbl_cor1.Text = "Correo Novia:";
                    }
                    else if (opc == "Ambos")
                    {
                        lbl_cor1.Text = "Correo Novio:";
                        lbl_cor2.Visible = true;
                        txt_cor2.Visible = true;
                    }
                }
                else
                {
                    lbl_cor1.Text = "Correo Notif.:";
                }
            };

            cb_opc_cor.SelectedIndexChanged += (s, e) =>
            {
                if (cb_opc_cor.SelectedItem != null)
                {
                    cambiar_modo_correo(cb_opc_cor.SelectedItem.ToString());
                }
            };

            Action<string> actualizar_interfaz_registro = (eleccion) =>
            {
                string[] reqs = new string[0];
                if (eleccion == "Bautizo")
                {
                    reqs = new string[] { "1. Copia DNI Bautizado", "2. Partida Nacimiento", "3. Copia DNI Padre", "4. Copia DNI Madre", "5. Copia DNI Padrino", "6. Copia DNI Madrina" };
                    lblGen.Visible = true;
                    cb_gen.Visible = true;
                    lbl_d1.Text = "DNI Bautizado:";
                    AsignarPlaceholder(txt_nom1, "Nombre Bautizado");
                    lbl_d2.Text = "DNI Apoderado:";
                    AsignarPlaceholder(txt_nom2, "Nombre Apoderado");
                    lblNotif.Visible = false;
                    cb_opc_cor.Visible = false;
                    cambiar_modo_correo("N/A");
                }
                else if (eleccion == "Primera Comunión")
                {
                    reqs = new string[] { "1. Copia DNI Comulgante", "2. Partida Bautismo", "3. Copia DNI Padre", "4. Copia DNI Madre" };
                    lblGen.Visible = true;
                    cb_gen.Visible = true;
                    lbl_d1.Text = "DNI Comulgante:";
                    AsignarPlaceholder(txt_nom1, "Nombre Comulgante");
                    lbl_d2.Text = "DNI Apoderado:";
                    AsignarPlaceholder(txt_nom2, "Nombre Apoderado");
                    lblNotif.Visible = false;
                    cb_opc_cor.Visible = false;
                    cambiar_modo_correo("N/A");
                }
                else if (eleccion == "Confirmación")
                {
                    reqs = new string[] { "1. Copia DNI Confirmando", "2. Partida Bautismo", "3. Const. 1ra Comunión", "4. Copia DNI Padrino/Madrina", "5. Const. Sacram Padrino" };
                    lblGen.Visible = true;
                    cb_gen.Visible = true;
                    lbl_d1.Text = "DNI Confirmando:";
                    AsignarPlaceholder(txt_nom1, "Nombre Confirmando");
                    lbl_d2.Text = "DNI Apoderado:";
                    AsignarPlaceholder(txt_nom2, "Nombre Apoderado");
                    lblNotif.Visible = false;
                    cb_opc_cor.Visible = false;
                    cambiar_modo_correo("N/A");
                }
                else if (eleccion == "Matrimonio Regular")
                {
                    reqs = new string[] { "1. Copia DNI Novio", "2. Copia DNI Novia", "3. Partida Bautismo Novio", "4. Partida Bautismo Novia", "5. Confirmación Novio", "6. Confirmación Novia", "7. Copia DNI Testigo 1", "8. Copia DNI Testigo 2" };
                    lblGen.Visible = false;
                    cb_gen.Visible = false;
                    lbl_d1.Text = "DNI Novio:";
                    AsignarPlaceholder(txt_nom1, "Nombre Novio");
                    lbl_d2.Text = "DNI Novia:";
                    AsignarPlaceholder(txt_nom2, "Nombre Novia");
                    lblNotif.Visible = true;
                    cb_opc_cor.Visible = true;
                    cb_opc_cor.SelectedItem = "Ambos";
                    cambiar_modo_correo("Ambos");
                }

                this.reqs_activos = reqs.Length;
                for (int j = 0; j < this.chks.Count; j++)
                {
                    if (j < reqs.Length)
                    {
                        this.chks[j].Text = reqs[j];
                        this.chks[j].Visible = true;
                    }
                    else
                    {
                        this.chks[j].Visible = false;
                        this.chks[j].Checked = false;
                    }
                }
                actualizar_obs_auto();
            };

            cb_tipo.SelectedIndexChanged += (s, e) =>
            {
                if (cb_tipo.SelectedItem != null)
                {
                    actualizar_interfaz_registro(cb_tipo.SelectedItem.ToString());
                }
            };
            cb_tipo.SelectedItem = "Bautizo";

            Button btnGuardarExp = CrearBotonAccion("Inscribir y Guardar Expediente", ColorTranslator.FromHtml("#2563eb"), new Point(285, 630), new Size(350, 50), 13f);

            btnGuardarExp.Click += (s, e) =>
            {
                string dni1_val = txt_d1.Text.Trim();
                string dni2_val = txt_d2.Text.Trim();
                string nom1_val = Regex.Replace(txt_nom1.Text.Trim(), @"\s+", " ");
                string nom2_val = Regex.Replace(txt_nom2.Text.Trim(), @"\s+", " ");
                string tipo_sel = cb_tipo.SelectedItem.ToString();

                if (dni1_val == "" && dni2_val == "" && nom1_val == "" && nom2_val == "")
                {
                    MessageBox.Show("No se han llenado los datos del feligrés.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                List<string> errores = new List<string>();
                if (dni1_val.Length != 8 || dni1_val == "00000000")
                {
                    errores.Add("- El DNI del titular debe tener 8 dígitos numéricos válidos.");
                }
                if (!this.EsNombreValido(nom1_val))
                {
                    errores.Add("- El Nombre del titular debe contener al menos 2 letras válidas.");
                }

                if (dni2_val.Length != 8 || dni2_val == "00000000")
                {
                    if (tipo_sel == "Matrimonio Regular")
                    {
                        errores.Add("- El DNI de la Novia debe tener 8 dígitos numéricos válidos.");
                    }
                    else
                    {
                        errores.Add("- El DNI del Apoderado es obligatorio (8 dígitos numéricos válidos).");
                    }
                }
                if (!this.EsNombreValido(nom2_val))
                {
                    if (tipo_sel == "Matrimonio Regular")
                    {
                        errores.Add("- El Nombre de la Novia debe contener al menos 2 letras válidas.");
                    }
                    else
                    {
                        errores.Add("- El Nombre del Apoderado debe contener al menos 2 letras válidas.");
                    }
                }

                if (dni1_val != "" && dni1_val == dni2_val)
                {
                    errores.Add("- No se pueden colocar 2 DNIs iguales. El DNI del titular y el secundario deben ser diferentes.");
                }
                if (nom1_val != "" && nom1_val.ToLower() == nom2_val.ToLower())
                {
                    errores.Add("- El nombre del titular y el nombre secundario no pueden ser iguales.");
                }

                for (int idxExist = 0; idxExist < this.db.dnis_prin.Count; idxExist++)
                {
                    if (this.db.dnis_prin[idxExist] == dni1_val && this.db.tipos[idxExist] == tipo_sel)
                    {
                        errores.Add("- El DNI " + dni1_val + " ya se encuentra registrado en " + tipo_sel + ".");
                        break;
                    }
                    if (tipo_sel == "Matrimonio Regular" && (this.db.dnis_sec[idxExist] == dni2_val || this.db.dnis_prin[idxExist] == dni2_val) && this.db.tipos[idxExist] == tipo_sel)
                    {
                        errores.Add("- El DNI " + dni2_val + " ya se encuentra registrado en " + tipo_sel + ".");
                        break;
                    }
                }

                if (tipo_sel == "Primera Comunión")
                {
                    if (this.config.fecha_comunion == "--/--")
                    {
                        errores.Add("- Debe configurar la fecha de inicio de Catequesis en 'Configuración de Fechas' antes de registrar.");
                    }
                }

                if (tipo_sel == "Confirmación")
                {
                    if (this.config.fecha_confirmacion == "--/--")
                    {
                        errores.Add("- Debe configurar la fecha de inicio de Catequesis en 'Configuración de Fechas' antes de registrar.");
                    }
                }

                string nom_final = nom1_val;
                string gen_final = "";
                string cor_final = "";

                if (tipo_sel == "Matrimonio Regular")
                {
                    gen_final = "N/A";
                    List<string> correos_validos = new List<string>();
                    string opc = cb_opc_cor.SelectedItem.ToString();

                    if (opc == "Solo Novio" || opc == "Ambos")
                    {
                        string val_c1 = txt_cor1.Text.Trim();
                        if (val_c1 != "")
                        {
                            if (!this.logica.validar_correo_regex(val_c1))
                            {
                                errores.Add("- Correo del Novio inválido.");
                            }
                            else
                            {
                                correos_validos.Add(val_c1);
                            }
                        }
                    }

                    if (opc == "Solo Novia" || opc == "Ambos")
                    {
                        string val_c2 = txt_cor2.Text.Trim();
                        if (val_c2 != "")
                        {
                            if (!this.logica.validar_correo_regex(val_c2))
                            {
                                errores.Add("- Correo de la Novia inválido.");
                            }
                            else
                            {
                                correos_validos.Add(val_c2);
                            }
                        }
                    }

                    cor_final = string.Join(",", correos_validos);
                }
                else
                {
                    string val_c1 = txt_cor1.Text.Trim();
                    if (val_c1 != "")
                    {
                        if (!this.logica.validar_correo_regex(val_c1))
                        {
                            errores.Add("- El correo ingresado es inválido.");
                        }
                        else
                        {
                            cor_final = val_c1;
                        }
                    }
                    gen_final = cb_gen.SelectedItem.ToString();
                }

                string pago = cb_pago.SelectedItem.ToString();
                string deuda = "0";
                if (pago == "Falta Pagar")
                {
                    deuda = txt_deuda.Text.Trim();
                    if (deuda == "")
                    {
                        errores.Add("- Debe especificar el monto de la deuda.");
                    }
                    else
                    {
                        double monto_float = 0;
                        if (double.TryParse(deuda, NumberStyles.Any, CultureInfo.InvariantCulture, out monto_float))
                        {
                            if (monto_float <= 0)
                            {
                                errores.Add("- El monto de la deuda debe ser mayor a 0 si indica 'Falta Pagar'.");
                            }
                        }
                        else
                        {
                            errores.Add("- El monto de la deuda no es un número válido.");
                        }
                    }
                }

                string obs_adic_txt = "";
                if (chk_adic.Checked)
                {
                    obs_adic_txt = txt_obs_adic.Text.Trim();
                    if (obs_adic_txt == "")
                    {
                        errores.Add("- Ha marcado añadir observación adicional manual, pero la caja está vacía.");
                    }
                }

                if (errores.Count > 0)
                {
                    MessageBox.Show("Se detectaron los siguientes errores:\n\n" + string.Join("\n", errores), "Datos Incompletos o Inválidos", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                bool completos = true;
                for (int k = 0; k < this.reqs_activos; k++)
                {
                    if (!this.chks[k].Checked)
                    {
                        completos = false;
                    }
                }

                string est = "";
                if (completos && pago == "Pagado")
                {
                    est = "Inscrito(a)";
                }
                else
                {
                    est = "Observado";
                }

                string obs_auto_txt = txt_obs_auto.Text.Trim();

                DialogResult res_conf = MessageBox.Show("¿Está seguro de inscribir y guardar este expediente?", "Confirmar", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (res_conf != DialogResult.Yes)
                {
                    return;
                }

                List<string> lista_chks = new List<string>();
                for (int k = 0; k < this.reqs_activos; k++)
                {
                    if (this.chks[k].Checked)
                    {
                        lista_chks.Add("1");
                    }
                    else
                    {
                        lista_chks.Add("0");
                    }
                }
                string chks_str = string.Join(",", lista_chks);

                this.db.registrar(dni1_val, dni2_val, nom_final, nom2_val, cor_final, gen_final, tipo_sel, est, obs_auto_txt, obs_adic_txt, chks_str, "Por Programar", "---", "---", "---", pago, deuda);

                if (pago == "Pagado")
                {
                    if (completos)
                    {
                        string dir_temp = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "exportaciones");
                        if (!Directory.Exists(dir_temp))
                        {
                            Directory.CreateDirectory(dir_temp);
                        }
                        string ruta_pdf = Path.Combine(dir_temp, "Constancia_" + dni1_val + ".pdf");

                        string dni_impreso = "";
                        if (tipo_sel == "Matrimonio Regular")
                        {
                            dni_impreso = dni1_val + ", " + dni2_val;
                        }
                        else
                        {
                            dni_impreso = dni1_val;
                        }

                        this.logica.generar_constancia_inscripcion(ruta_pdf, nom_final, nom2_val, dni_impreso, gen_final, tipo_sel, this.config);

                        if (cor_final != "")
                        {
                            this.logica.enviar_correo(cor_final, nom_final, nom2_val, tipo_sel, est, obs_auto_txt, obs_adic_txt, pago, deuda, ruta_pdf);
                            MessageBox.Show("Inscrito Exitosamente. Constancia enviada a los correos.", "Éxito", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                        else
                        {
                            MessageBox.Show("Inscrito Exitosamente. Constancia guardada en 'exportaciones'.", "Éxito", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                    }
                    else
                    {
                        if (cor_final != "")
                        {
                            this.logica.enviar_correo(cor_final, nom_final, nom2_val, tipo_sel, est, obs_auto_txt, obs_adic_txt, pago, deuda);
                            MessageBox.Show("Registrado. Se envió notificación al correo (Sin PDF).", "Éxito", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                        else
                        {
                            MessageBox.Show("Registrado correctamente (Pendiente de regularizar).", "Éxito", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                    }
                }
                else
                {
                    if (cor_final != "")
                    {
                        this.logica.enviar_correo(cor_final, nom_final, nom2_val, tipo_sel, est, obs_auto_txt, obs_adic_txt, pago, deuda);
                        MessageBox.Show("Registrado. Se envió notificación al correo (Sin PDF).", "Éxito", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else
                    {
                        MessageBox.Show("Registrado correctamente (Pendiente de regularizar).", "Éxito", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }

                this.mostrar_dashboard();
            };

            f_main.Controls.Add(btnGuardarExp);
            this.content_frame.Controls.Add(f_main);
        }

        // ================= 6. BÚSQUEDA Y EDICIÓN =================
        private void mostrar_buscar()
        {
            this.limpiar_contenido();

            Label lblTit = new Label();
            lblTit.Text = "Buscador de Expedientes";
            lblTit.Font = new Font("Segoe UI", 20f, FontStyle.Bold);
            lblTit.ForeColor = ColorTranslator.FromHtml("#2563eb");
            lblTit.Location = new Point(40, 15);
            lblTit.AutoSize = true;
            this.content_frame.Controls.Add(lblTit);

            Panel f_top = new Panel();
            f_top.Location = new Point(40, 60);
            f_top.Size = new Size(920, 60);
            f_top.BackColor = Color.White;
            f_top.BorderStyle = BorderStyle.FixedSingle;

            Label lblBusq = new Label();
            lblBusq.Text = "DNI del Feligrés:";
            lblBusq.Location = new Point(20, 20);
            lblBusq.AutoSize = true;
            f_top.Controls.Add(lblBusq);

            TextBox txt_bus = new TextBox();
            txt_bus.Location = new Point(140, 17);
            txt_bus.Width = 220;
            this.adjuntar_validacion(txt_bus, "dni");
            f_top.Controls.Add(txt_bus);

            Button btnConsultar = CrearBotonAccion("Consultar", ColorTranslator.FromHtml("#10b981"), new Point(380, 13), new Size(130, 34));
            f_top.Controls.Add(btnConsultar);

            this.content_frame.Controls.Add(f_top);

            Panel f_edit = new Panel();
            f_edit.Location = new Point(40, 130);
            f_edit.Size = new Size(920, 650);
            f_edit.BackColor = Color.White;
            f_edit.BorderStyle = BorderStyle.FixedSingle;
            f_edit.Visible = false;

            int idx_actual = -1;

            Label lbl_e_dni1 = new Label() { Text = "DNI Titular:", Location = new Point(20, 20), Size = new Size(125, 25) };
            TextBox t_dni1 = new TextBox() { Location = new Point(150, 18), Width = 220 };
            this.adjuntar_validacion(t_dni1, "dni");

            Label lbl_e_dni2 = new Label() { Text = "DNI Apoderado:", Location = new Point(400, 20), Size = new Size(125, 25) };
            TextBox t_dni2 = new TextBox() { Location = new Point(530, 18), Width = 220 };
            this.adjuntar_validacion(t_dni2, "dni");

            Label lbl_e_nom1 = new Label() { Text = "Titular:", Location = new Point(20, 55), Size = new Size(125, 25) };
            TextBox t_nom = new TextBox() { Location = new Point(150, 53), Width = 220 };
            this.adjuntar_validacion(t_nom, "texto");

            Label lbl_e_nom2 = new Label() { Text = "Apoderado:", Location = new Point(400, 55), Size = new Size(125, 25) };
            TextBox t_nom2 = new TextBox() { Location = new Point(530, 53), Width = 220 };
            this.adjuntar_validacion(t_nom2, "texto");

            Label lbl_e_cor = new Label() { Text = "Correo(s):", Location = new Point(20, 90), Size = new Size(125, 25) };
            TextBox t_cor = new TextBox() { Location = new Point(150, 88), Width = 400 };
            this.adjuntar_validacion(t_cor, "correo");

            Label lbl_e_est = new Label() { Text = "Estado:", Location = new Point(20, 125), Size = new Size(125, 25) };
            ComboBox c_est = new ComboBox() { DropDownStyle = ComboBoxStyle.DropDownList, Location = new Point(150, 123), Width = 150 };
            c_est.Items.AddRange(new string[] { "Inscrito(a)", "Observado", "Finalizado" });

            Label lbl_e_pag = new Label() { Text = "Pago:", Location = new Point(320, 125), Size = new Size(50, 25) };
            ComboBox c_pag = new ComboBox() { DropDownStyle = ComboBoxStyle.DropDownList, Location = new Point(375, 123), Width = 130 };
            c_pag.Items.AddRange(new string[] { "Pagado", "Falta Pagar" });

            TextBox t_deu = new TextBox() { Location = new Point(520, 123), Width = 90 };
            this.adjuntar_validacion(t_deu, "numero");

            f_edit.Controls.AddRange(new Control[] { lbl_e_dni1, t_dni1, lbl_e_dni2, t_dni2, lbl_e_nom1, t_nom, lbl_e_nom2, t_nom2, lbl_e_cor, t_cor, lbl_e_est, c_est, lbl_e_pag, c_pag, t_deu });

            Panel f_req_edit = new Panel();
            f_req_edit.Location = new Point(20, 165);
            f_req_edit.Size = new Size(860, 155);
            f_req_edit.BackColor = ColorTranslator.FromHtml("#f8fafc");
            f_req_edit.BorderStyle = BorderStyle.FixedSingle;

            Label lblReqEditTit = new Label() { Text = "Requisitos de Documentación:", Font = new Font("Segoe UI", 10f, FontStyle.Bold), Location = new Point(15, 10), AutoSize = true };
            f_req_edit.Controls.Add(lblReqEditTit);

            this.chks_edit.Clear();
            for (int i = 0; i < 8; i++)
            {
                CheckBox chk = new CheckBox();
                chk.AutoSize = true;
                int col = i % 2;
                int fila = i / 2;
                chk.Location = new Point(20 + (col * 400), 38 + (fila * 26));
                this.chks_edit.Add(chk);
                f_req_edit.Controls.Add(chk);
            }
            f_edit.Controls.Add(f_req_edit);

            Panel f_obs_izq_edit = new Panel() { Location = new Point(20, 330), Size = new Size(420, 165), BackColor = ColorTranslator.FromHtml("#f1f5f9"), BorderStyle = BorderStyle.FixedSingle };
            Label lblObsGenE = new Label() { Text = "Observación Generada:", Font = new Font("Segoe UI", 10f, FontStyle.Bold), Location = new Point(15, 10), AutoSize = true };
            TextBox txt_obs_auto_edit = new TextBox() { Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical, Location = new Point(15, 35), Size = new Size(390, 115), BackColor = Color.White };
            f_obs_izq_edit.Controls.Add(lblObsGenE);
            f_obs_izq_edit.Controls.Add(txt_obs_auto_edit);
            f_edit.Controls.Add(f_obs_izq_edit);

            Panel f_obs_der_edit = new Panel() { Location = new Point(460, 330), Size = new Size(420, 165), BackColor = ColorTranslator.FromHtml("#f1f5f9"), BorderStyle = BorderStyle.FixedSingle };
            CheckBox chk_adic_edit = new CheckBox() { Text = "¿Añadir/Ver observación adicional manual?", Font = new Font("Segoe UI", 10f, FontStyle.Bold), Location = new Point(15, 10), AutoSize = true };
            TextBox txt_obs_adic_edit = new TextBox() { Multiline = true, ScrollBars = ScrollBars.Vertical, Location = new Point(15, 35), Size = new Size(390, 115), Visible = false };
            f_obs_der_edit.Controls.Add(chk_adic_edit);
            f_obs_der_edit.Controls.Add(txt_obs_adic_edit);
            f_edit.Controls.Add(f_obs_der_edit);

            Label lblFecOf = new Label() { Text = "Fecha Oficial:", Location = new Point(20, 515), Size = new Size(100, 25) };
            TextBox t_fec = new TextBox() { Location = new Point(125, 513), Width = 140 };
            AsignarPlaceholder(t_fec, "DD/MM/AAAA");
            this.adjuntar_validacion(t_fec, "fecha");

            Label lblLib = new Label() { Text = "Libro:", Location = new Point(285, 515), AutoSize = true };
            TextBox t_l = new TextBox() { Location = new Point(335, 513), Width = 60 };
            AsignarPlaceholder(t_l, "---");
            this.adjuntar_validacion(t_l, "registro_libro");

            Label lblFol = new Label() { Text = "Folio:", Location = new Point(415, 515), AutoSize = true };
            TextBox t_f = new TextBox() { Location = new Point(465, 513), Width = 60 };
            AsignarPlaceholder(t_f, "---");
            this.adjuntar_validacion(t_f, "registro_libro");

            Label lblAsi = new Label() { Text = "Asiento:", Location = new Point(545, 515), AutoSize = true };
            TextBox t_a = new TextBox() { Location = new Point(610, 513), Width = 60 };
            AsignarPlaceholder(t_a, "---");
            this.adjuntar_validacion(t_a, "registro_libro");

            f_edit.Controls.AddRange(new Control[] { lblFecOf, t_fec, lblLib, t_l, lblFol, t_f, lblAsi, t_a });

            Button btn_act = CrearBotonAccion("Actualizar Datos", ColorTranslator.FromHtml("#2563eb"), new Point(180, 570), new Size(180, 42));
            Button btn_env = CrearBotonAccion("Enviar por Correo", ColorTranslator.FromHtml("#10b981"), new Point(380, 570), new Size(180, 42));
            Button btn_eli = CrearBotonAccion("Eliminar", ColorTranslator.FromHtml("#ef4444"), new Point(580, 570), new Size(150, 42));

            f_edit.Controls.AddRange(new Control[] { btn_act, btn_env, btn_eli });
            this.content_frame.Controls.Add(f_edit);

            Action actualizar_obs_auto_edit = () =>
            {
                List<string> faltantes = new List<string>();
                for (int j = 0; j < this.reqs_activos_edit; j++)
                {
                    if (!this.chks_edit[j].Checked)
                    {
                        string texto_limpio = this.chks_edit[j].Text.Substring(3);
                        faltantes.Add(texto_limpio);
                    }
                }

                string texto = "";
                if (faltantes.Count > 0)
                {
                    texto = "Faltan los siguientes documentos físicos:\r\n- " + string.Join("\r\n- ", faltantes) + "\r\n";
                }
                else
                {
                    texto = "Documentación física completa.\r\n";
                }

                if (c_pag.SelectedItem != null && c_pag.SelectedItem.ToString() == "Falta Pagar")
                {
                    texto = texto + "Pendiente de pago por regularizar.";
                }

                txt_obs_auto_edit.Text = texto.Trim();

                if (faltantes.Count == 0 && c_pag.SelectedItem != null && c_pag.SelectedItem.ToString() == "Pagado")
                {
                    if (c_est.SelectedItem != null && c_est.SelectedItem.ToString() == "Observado")
                    {
                        c_est.SelectedItem = "Inscrito(a)";
                    }
                }

                if (faltantes.Count > 0 || (c_pag.SelectedItem != null && c_pag.SelectedItem.ToString() == "Falta Pagar"))
                {
                    if (c_est.SelectedItem != null && c_est.SelectedItem.ToString() == "Inscrito(a)")
                    {
                        c_est.SelectedItem = "Observado";
                    }
                }
            };

            for (int i = 0; i < 8; i++)
            {
                this.chks_edit[i].CheckedChanged += (s, e) => actualizar_obs_auto_edit();
            }

            c_pag.SelectedIndexChanged += (s, e) =>
            {
                if (c_pag.SelectedItem != null && c_pag.SelectedItem.ToString() == "Falta Pagar")
                {
                    t_deu.Visible = true;
                }
                else
                {
                    t_deu.Visible = false;
                    t_deu.Text = "";
                }
                actualizar_obs_auto_edit();
            };

            chk_adic_edit.CheckedChanged += (s, e) =>
            {
                if (chk_adic_edit.Checked)
                {
                    txt_obs_adic_edit.Visible = true;
                }
                else
                {
                    txt_obs_adic_edit.Visible = false;
                    txt_obs_adic_edit.Text = "";
                }
            };

            Action<bool> buscar = (mostrarAlerta) =>
            {
                string dni_val = txt_bus.Text.Trim();
                if (dni_val == "")
                {
                    MessageBox.Show("No ha ingresado un DNI.", "Error de Búsqueda", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                if (dni_val.Length != 8 || dni_val == "00000000")
                {
                    MessageBox.Show("El DNI ingresado es incorrecto. Debe contener exactamente 8 dígitos numéricos válidos.", "Error de Búsqueda", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                int idx = this.db.buscar_por_dni(dni_val);
                if (idx != -1)
                {
                    if (this.db.checks_mem[idx].StartsWith("HIST|"))
                    {
                        f_edit.Visible = false;
                        MessageBox.Show("El DNI ingresado corresponde a una Constancia Antigua de " + this.db.nombres[idx] + ".\n\nPor favor, edite o gestione este registro desde el módulo 'Opciones Avanzadas'.", "Constancia Antigua Detectada", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        return;
                    }

                    idx_actual = idx;
                    f_edit.Visible = true;

                    string tipo = this.db.tipos[idx];
                    string[] reqs = new string[0];
                    if (tipo == "Bautizo")
                    {
                        reqs = new string[] { "1. Copia DNI Bautizado", "2. Partida Nacimiento", "3. Copia DNI Padre", "4. Copia DNI Madre", "5. Copia DNI Padrino", "6. Copia DNI Madrina" };
                        lbl_e_dni1.Text = "DNI Bautizado:";
                        lbl_e_dni2.Text = "DNI Apoderado:";
                        lbl_e_nom1.Text = "Bautizado:";
                        lbl_e_nom2.Text = "Apoderado:";
                    }
                    else if (tipo == "Primera Comunión")
                    {
                        reqs = new string[] { "1. Copia DNI Comulgante", "2. Partida Bautismo", "3. Copia DNI Padre", "4. Copia DNI Madre" };
                        lbl_e_dni1.Text = "DNI Comulgante:";
                        lbl_e_dni2.Text = "DNI Apoderado:";
                        lbl_e_nom1.Text = "Comulgante:";
                        lbl_e_nom2.Text = "Apoderado:";
                    }
                    else if (tipo == "Confirmación")
                    {
                        reqs = new string[] { "1. Copia DNI Confirmando", "2. Partida Bautismo", "3. Const. 1ra Comunión", "4. Copia DNI Padrino/Madrina", "5. Const. Sacram Padrino" };
                        lbl_e_dni1.Text = "DNI Confirmando:";
                        lbl_e_dni2.Text = "DNI Apoderado:";
                        lbl_e_nom1.Text = "Confirmando:";
                        lbl_e_nom2.Text = "Apoderado:";
                    }
                    else if (tipo == "Matrimonio Regular")
                    {
                        reqs = new string[] { "1. Copia DNI Novio", "2. Copia DNI Novia", "3. Partida Bautismo Novio", "4. Partida Bautismo Novia", "5. Confirmación Novio", "6. Confirmación Novia", "7. Copia DNI Testigo 1", "8. Copia DNI Testigo 2" };
                        lbl_e_dni1.Text = "DNI Novio:";
                        lbl_e_dni2.Text = "DNI Novia:";
                        lbl_e_nom1.Text = "Novio:";
                        lbl_e_nom2.Text = "Novia:";
                    }

                    this.reqs_activos_edit = reqs.Length;
                    string[] memoria = this.db.checks_mem[idx].Split(',');
                    for (int k = 0; k < this.chks_edit.Count; k++)
                    {
                        if (k < reqs.Length)
                        {
                            this.chks_edit[k].Text = reqs[k];
                            this.chks_edit[k].Visible = true;
                            if (k < memoria.Length && memoria[k] == "1")
                            {
                                this.chks_edit[k].Checked = true;
                            }
                            else
                            {
                                this.chks_edit[k].Checked = false;
                            }
                        }
                        else
                        {
                            this.chks_edit[k].Visible = false;
                            this.chks_edit[k].Checked = false;
                        }
                    }

                    t_dni1.Text = this.db.dnis_prin[idx];
                    t_dni2.Text = this.db.dnis_sec[idx];
                    t_nom.Text = this.db.nombres[idx];
                    t_nom2.Text = this.db.nombres_sec[idx];
                    t_cor.Text = this.db.correos[idx];
                    c_est.SelectedItem = this.db.estados[idx];
                    c_pag.SelectedItem = this.db.pagos[idx];
                    t_deu.Text = this.db.deudas[idx];

                    if (this.db.pagos[idx] == "Pagado")
                    {
                        t_deu.Visible = false;
                    }
                    else
                    {
                        t_deu.Visible = true;
                    }

                    if (this.db.obs_adic[idx] != "")
                    {
                        chk_adic_edit.Checked = true;
                        txt_obs_adic_edit.Text = this.db.obs_adic[idx];
                    }
                    else
                    {
                        chk_adic_edit.Checked = false;
                        txt_obs_adic_edit.Text = "";
                    }

                    if (this.db.fechas_sac[idx] != "Por Programar")
                    {
                        t_fec.Text = this.db.fechas_sac[idx];
                    }
                    else
                    {
                        t_fec.Text = "";
                    }

                    t_l.Text = (this.db.libros[idx] != "---") ? this.db.libros[idx] : "";
                    t_f.Text = (this.db.folios[idx] != "---") ? this.db.folios[idx] : "";
                    t_a.Text = (this.db.asientos[idx] != "---") ? this.db.asientos[idx] : "";

                    actualizar_obs_auto_edit();
                    f_edit.Invalidate();

                    if (mostrarAlerta)
                    {
                        MessageBox.Show("Expediente de " + this.db.nombres[idx] + " encontrado.", "Búsqueda Exitosa", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
                else
                {
                    f_edit.Visible = false;
                    MessageBox.Show("No se encontraron expedientes con ese DNI.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            };

            btnConsultar.Click += (s, e) => buscar(true);

            Action<bool> ejecutar_accion = (enviar_correo) =>
            {
                string dni1_val = t_dni1.Text.Trim();
                string dni2_val = t_dni2.Text.Trim();
                string nom1_val = Regex.Replace(t_nom.Text.Trim(), @"\s+", " ");
                string nom2_val = Regex.Replace(t_nom2.Text.Trim(), @"\s+", " ");

                if (dni1_val.Length != 8 || dni1_val == "00000000")
                {
                    MessageBox.Show("El DNI principal debe tener 8 dígitos numéricos válidos.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                if (dni2_val.Length != 8 || dni2_val == "00000000")
                {
                    MessageBox.Show("El DNI secundario debe tener 8 dígitos numéricos válidos.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                if (dni1_val == dni2_val)
                {
                    MessageBox.Show("No se pueden colocar 2 DNIs iguales. El DNI del titular y el secundario deben ser diferentes.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                if (!this.EsNombreValido(nom1_val))
                {
                    MessageBox.Show("El nombre del titular debe contener al menos 2 letras válidas.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                if (!this.EsNombreValido(nom2_val))
                {
                    MessageBox.Show("El nombre secundario debe contener al menos 2 letras válidas.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                if (nom1_val.ToLower() == nom2_val.ToLower())
                {
                    MessageBox.Show("El nombre del titular y el nombre secundario no pueden ser iguales.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                int i = idx_actual;

                for (int idxExist = 0; idxExist < this.db.dnis_prin.Count; idxExist++)
                {
                    if (idxExist != i && this.db.dnis_prin[idxExist] == dni1_val && this.db.tipos[idxExist] == this.db.tipos[i])
                    {
                        MessageBox.Show("Ya existe otro expediente de " + this.db.tipos[i] + " con el DNI " + dni1_val + ".", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                }

                string correos_edit = t_cor.Text.Trim();
                if (correos_edit != "")
                {
                    string[] lista_correos = correos_edit.Split(',');
                    for (int c = 0; c < lista_correos.Length; c++)
                    {
                        if (!this.logica.validar_correo_regex(lista_correos[c].Trim()))
                        {
                            MessageBox.Show("El correo '" + lista_correos[c].Trim() + "' no es válido.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return;
                        }
                    }
                }

                if (this.db.tipos[i] == "Primera Comunión")
                {
                    if (this.config.fecha_comunion == "--/--")
                    {
                        MessageBox.Show("Debe configurar la fecha de inicio de Catequesis en 'Configuración de Fechas' antes de actualizar.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                }

                if (this.db.tipos[i] == "Confirmación")
                {
                    if (this.config.fecha_confirmacion == "--/--")
                    {
                        MessageBox.Show("Debe configurar la fecha de inicio de Catequesis en 'Configuración de Fechas' antes de actualizar.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                }

                string f_v = t_fec.Text.Trim();
                if (f_v == "")
                {
                    f_v = "Por Programar";
                }
                else
                {
                    DateTime fecha_obj;
                    if (DateTime.TryParseExact(f_v, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out fecha_obj))
                    {
                        if (fecha_obj.Date < DateTime.Now.Date)
                        {
                            MessageBox.Show("La Fecha Oficial programada no puede ser una fecha antigua (pasada).", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return;
                        }
                    }
                    else
                    {
                        MessageBox.Show("El formato de la Fecha Oficial debe ser una fecha válida: DD/MM/AAAA.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                }

                string estado_elegido = c_est.SelectedItem.ToString();
                string pago_elegido = c_pag.SelectedItem.ToString();

                string deuda_nueva = "0";
                if (pago_elegido == "Falta Pagar")
                {
                    deuda_nueva = t_deu.Text.Trim();
                    if (deuda_nueva == "")
                    {
                        MessageBox.Show("Debe especificar el monto de la deuda.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                    double deuda_float = 0;
                    if (double.TryParse(deuda_nueva, NumberStyles.Any, CultureInfo.InvariantCulture, out deuda_float))
                    {
                        if (deuda_float <= 0)
                        {
                            MessageBox.Show("El monto de la deuda debe ser mayor a 0 si indica 'Falta Pagar'.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return;
                        }
                    }
                    else
                    {
                        MessageBox.Show("El monto de la deuda no es válido.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                }

                string obs_auto_txt = txt_obs_auto_edit.Text.Trim();
                string obs_adic_txt = "";
                if (chk_adic_edit.Checked)
                {
                    obs_adic_txt = txt_obs_adic_edit.Text.Trim();
                    if (obs_adic_txt == "")
                    {
                        MessageBox.Show("Ha marcado añadir observación adicional manual, pero la caja está vacía.", "Atención", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                }

                bool completos = true;
                for (int j = 0; j < this.reqs_activos_edit; j++)
                {
                    if (!this.chks_edit[j].Checked)
                    {
                        completos = false;
                    }
                }

                string l_v = t_l.Text.Trim();
                if (l_v != "" && !this.EsNumeroPositivoValido(l_v))
                {
                    MessageBox.Show("El Libro solo admite números enteros positivos.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                if (l_v == "")
                {
                    l_v = "---";
                }

                string f_val = t_f.Text.Trim();
                if (f_val != "" && !this.EsNumeroPositivoValido(f_val))
                {
                    MessageBox.Show("El Folio solo admite números enteros positivos.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                if (f_val == "")
                {
                    f_val = "---";
                }

                string a_v = t_a.Text.Trim();
                if (a_v != "" && !this.EsNumeroPositivoValido(a_v))
                {
                    MessageBox.Show("El Asiento solo admite números enteros positivos.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                if (a_v == "")
                {
                    a_v = "---";
                }

                if (estado_elegido == "Inscrito(a)")
                {
                    if (!completos || pago_elegido == "Falta Pagar")
                    {
                        MessageBox.Show("No se puede establecer el estado como 'Inscrito(a)' si faltan requisitos físicos o tiene deuda pendiente.", "Estado Inconsistente", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                }

                if (estado_elegido == "Finalizado")
                {
                    if (!completos || pago_elegido == "Falta Pagar")
                    {
                        MessageBox.Show("Para marcar un expediente como 'Finalizado', debe tener todos los requisitos completos y estar 'Pagado'.", "Requisitos Incompletos", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                    if (l_v == "---" || f_val == "---" || a_v == "---")
                    {
                        MessageBox.Show("Para finalizar el expediente y emitir la partida oficial, debe completar los números positivos de Libro, Folio y Asiento.", "Datos de Registro Faltantes", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                }

                List<string> lista_chks_e = new List<string>();
                for (int j = 0; j < this.reqs_activos_edit; j++)
                {
                    if (this.chks_edit[j].Checked)
                    {
                        lista_chks_e.Add("1");
                    }
                    else
                    {
                        lista_chks_e.Add("0");
                    }
                }
                string chks_str = string.Join(",", lista_chks_e);

                this.db.actualizar_completo(i, dni1_val, dni2_val, nom1_val, nom2_val, correos_edit, this.db.generos[i], estado_elegido, obs_auto_txt, obs_adic_txt, chks_str, f_v, l_v, f_val, a_v, pago_elegido, deuda_nueva);

                if (!enviar_correo)
                {
                    MessageBox.Show("Datos actualizados en el sistema.", "Éxito", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    if (correos_edit == "")
                    {
                        MessageBox.Show("No hay ningún correo registrado. Los datos fueron guardados, pero no se enviará notificación.", "Atención", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        buscar(false);
                        return;
                    }

                    if (pago_elegido == "Pagado")
                    {
                        if (estado_elegido == "Inscrito(a)")
                        {
                            string dir_temp = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "exportaciones");
                            if (!Directory.Exists(dir_temp))
                            {
                                Directory.CreateDirectory(dir_temp);
                            }
                            string ruta_pdf = Path.Combine(dir_temp, "Constancia_" + this.db.dnis_prin[i] + ".pdf");

                            string dni_impreso = this.db.dnis_prin[i];
                            if (this.db.tipos[i] == "Matrimonio Regular")
                            {
                                dni_impreso = this.db.dnis_prin[i] + ", " + this.db.dnis_sec[i];
                            }

                            this.logica.generar_constancia_inscripcion(ruta_pdf, nom1_val, nom2_val, dni_impreso, this.db.generos[i], this.db.tipos[i], this.config);
                            this.logica.enviar_correo(correos_edit, nom1_val, nom2_val, this.db.tipos[i], estado_elegido, obs_auto_txt, obs_adic_txt, pago_elegido, deuda_nueva, ruta_pdf);
                            MessageBox.Show("Datos actualizados en el sistema y Constancia enviada a los correos.", "Éxito", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                        else if (estado_elegido == "Finalizado")
                        {
                            string dir_temp = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "exportaciones");
                            if (!Directory.Exists(dir_temp))
                            {
                                Directory.CreateDirectory(dir_temp);
                            }
                            string ruta_pdf = Path.Combine(dir_temp, "Partida_Oficial_" + this.db.dnis_prin[i] + ".pdf");
                            this.logica.generar_partida_generica(ruta_pdf, nom1_val, nom2_val, this.db.tipos[i], l_v, f_val, a_v);
                            this.logica.enviar_correo(correos_edit, nom1_val, nom2_val, this.db.tipos[i], estado_elegido, obs_auto_txt, obs_adic_txt, pago_elegido, deuda_nueva, ruta_pdf);
                            MessageBox.Show("Datos actualizados en el sistema y Partida enviada por correo a los titulares.", "Éxito", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                        else
                        {
                            this.logica.enviar_correo(correos_edit, nom1_val, nom2_val, this.db.tipos[i], estado_elegido, obs_auto_txt, obs_adic_txt, pago_elegido, deuda_nueva);
                            MessageBox.Show("Datos actualizados en el sistema y Notificación de actualización enviada (Sin PDF).", "Éxito", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                    }
                    else
                    {
                        this.logica.enviar_correo(correos_edit, nom1_val, nom2_val, this.db.tipos[i], estado_elegido, obs_auto_txt, obs_adic_txt, pago_elegido, deuda_nueva);
                        MessageBox.Show("Datos actualizados en el sistema y Notificación de actualización enviada (Sin PDF).", "Éxito", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
                buscar(false);
            };

            btn_act.Click += (s, e) =>
            {
                DialogResult res = MessageBox.Show("¿Está seguro de actualizar los datos en el sistema?", "Confirmar", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (res == DialogResult.Yes)
                {
                    ejecutar_accion(false);
                }
            };

            btn_env.Click += (s, e) =>
            {
                DialogResult res = MessageBox.Show("¿Está seguro de actualizar los datos y enviar por correo?", "Confirmar", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (res == DialogResult.Yes)
                {
                    ejecutar_accion(true);
                }
            };

            btn_eli.Click += (s, e) =>
            {
                DialogResult res = MessageBox.Show("¿Está seguro de eliminar permanentemente el registro del sistema?", "Confirmar", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                if (res == DialogResult.Yes)
                {
                    this.db.eliminar(idx_actual);
                    f_edit.Visible = false;
                    MessageBox.Show("Registro borrado correctamente.", "Eliminado", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            };
        }

        // ================= 7. REPORTES =================
        private void mostrar_reportes()
        {
            this.limpiar_contenido();

            Label lblTit = new Label();
            lblTit.Text = "Reporte General de Expedientes";
            lblTit.Font = new Font("Segoe UI", 20f, FontStyle.Bold);
            lblTit.ForeColor = ColorTranslator.FromHtml("#2563eb");
            lblTit.Location = new Point(40, 15);
            lblTit.AutoSize = true;
            this.content_frame.Controls.Add(lblTit);

            Panel f_filtros = new Panel();
            f_filtros.Location = new Point(40, 60);
            f_filtros.Size = new Size(920, 65);
            f_filtros.BackColor = ColorTranslator.FromHtml("#f1f5f9");
            f_filtros.BorderStyle = BorderStyle.FixedSingle;

            Label lblSac = new Label() { Text = "Sacramento:", Font = new Font("Segoe UI", 10f, FontStyle.Bold), Location = new Point(15, 22), AutoSize = true };
            ComboBox cb_filtro_sac = new ComboBox() { DropDownStyle = ComboBoxStyle.DropDownList, Location = new Point(110, 19), Width = 150 };
            cb_filtro_sac.Items.AddRange(new string[] { "Todos", "Bautizo", "Primera Comunión", "Confirmación", "Matrimonio Regular" });
            cb_filtro_sac.SelectedItem = "Todos";

            Label lblFecOf = new Label() { Text = "Fecha Oficial:\n(DD/MM/AAAA)", Font = new Font("Segoe UI", 9.5f, FontStyle.Bold), Location = new Point(280, 12), AutoSize = true };
            TextBox txt_filtro_fec = new TextBox() { Location = new Point(400, 19), Width = 110 };
            this.adjuntar_validacion(txt_filtro_fec, "fecha");

            Label lblOrd = new Label() { Text = "Ordenar:", Font = new Font("Segoe UI", 10f, FontStyle.Bold), Location = new Point(530, 22), AutoSize = true };
            ComboBox cb_orden = new ComboBox() { DropDownStyle = ComboBoxStyle.DropDownList, Location = new Point(600, 19), Width = 150 };
            cb_orden.Items.AddRange(new string[] { "Ascendente (A-Z)", "Descendente (Z-A)" });
            cb_orden.SelectedItem = "Ascendente (A-Z)";

            Button btnFiltrar = CrearBotonAccion("Aplicar Filtros", ColorTranslator.FromHtml("#2563eb"), new Point(770, 15), new Size(130, 34));

            f_filtros.Controls.AddRange(new Control[] { lblSac, cb_filtro_sac, lblFecOf, txt_filtro_fec, lblOrd, cb_orden, btnFiltrar });
            this.content_frame.Controls.Add(f_filtros);

            DataGridView tabla = new DataGridView();
            tabla.Location = new Point(40, 140);
            tabla.Size = new Size(920, 420);
            tabla.BackgroundColor = Color.White;
            tabla.BorderStyle = BorderStyle.FixedSingle;
            tabla.AllowUserToAddRows = false;
            tabla.ReadOnly = true;
            tabla.RowHeadersVisible = false;
            tabla.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            tabla.DefaultCellStyle.WrapMode = DataGridViewTriState.True;
            tabla.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;
            tabla.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 10f, FontStyle.Bold);
            tabla.DefaultCellStyle.Font = new Font("Segoe UI", 10f, FontStyle.Regular);

            tabla.Columns.Add("colTit", "Titular(es)");
            tabla.Columns.Add("colSac", "Sacramento");
            tabla.Columns.Add("colFec", "Fecha Oficial");
            tabla.Columns.Add("colEst", "Estado");
            tabla.Columns.Add("colDeu", "Deuda");

            this.content_frame.Controls.Add(tabla);

            Action<bool> actualizar_tabla = (validarFiltro) =>
            {
                string f_fec = txt_filtro_fec.Text.Trim();
                if (validarFiltro && f_fec != "")
                {
                    DateTime dtFiltro;
                    if (f_fec.Length != 10 || !DateTime.TryParseExact(f_fec, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out dtFiltro))
                    {
                        MessageBox.Show("La Fecha Oficial ingresada en el filtro no es válida. Ingrese una fecha real con formato DD/MM/AAAA o deje el campo vacío.", "Fecha de Filtro Inválida", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                }

                tabla.Rows.Clear();

                if (cb_orden.SelectedItem.ToString() == "Ascendente (A-Z)")
                {
                    this.db.ordenar_bubble_asc();
                }
                else
                {
                    this.db.ordenar_selection_desc();
                }

                string f_sac = cb_filtro_sac.SelectedItem.ToString();

                for (int i = 0; i < this.db.nombres.Count; i++)
                {
                    if (f_sac != "Todos" && this.db.tipos[i] != f_sac)
                    {
                        continue;
                    }
                    if (f_fec != "" && this.db.fechas_sac[i] != f_fec)
                    {
                        continue;
                    }

                    string nombre_display = this.db.nombres[i];
                    if (this.db.tipos[i] == "Matrimonio Regular")
                    {
                        nombre_display = this.db.nombres[i] + ",\n" + this.db.nombres_sec[i];
                    }

                    string texto_est = this.db.estados[i] + "\n(" + this.db.pagos[i] + ")";
                    string deuda_val = this.db.deudas[i];
                    string texto_deuda = "No tiene deuda";
                    if (deuda_val != "0" && deuda_val != "")
                    {
                        texto_deuda = "S/ " + deuda_val;
                    }

                    int rowIdx = tabla.Rows.Add(nombre_display, this.db.tipos[i], this.db.fechas_sac[i], texto_est, texto_deuda);

                    if (this.db.estados[i] == "Finalizado")
                    {
                        tabla.Rows[rowIdx].Cells[3].Style.ForeColor = ColorTranslator.FromHtml("#10b981");
                    }
                    else if (this.db.estados[i] == "Inscrito(a)")
                    {
                        tabla.Rows[rowIdx].Cells[3].Style.ForeColor = ColorTranslator.FromHtml("#f59e0b");
                    }
                    else
                    {
                        tabla.Rows[rowIdx].Cells[3].Style.ForeColor = ColorTranslator.FromHtml("#ef4444");
                    }

                    if (texto_deuda == "No tiene deuda")
                    {
                        tabla.Rows[rowIdx].Cells[4].Style.ForeColor = ColorTranslator.FromHtml("#10b981");
                    }
                    else
                    {
                        tabla.Rows[rowIdx].Cells[4].Style.ForeColor = ColorTranslator.FromHtml("#ef4444");
                    }
                }

                if (validarFiltro && tabla.Rows.Count == 0)
                {
                    MessageBox.Show("No se encontraron expedientes con los filtros actuales.", "Sin Resultados", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            };

            btnFiltrar.Click += (s, e) => actualizar_tabla(true);
            actualizar_tabla(false);

            Button btnExportar = CrearBotonAccion("Exportar Resumen PDF", ColorTranslator.FromHtml("#10b981"), new Point(375, 580), new Size(250, 44), 12f);

            btnExportar.Click += (s, e) =>
            {
                if (this.db.nombres.Count == 0)
                {
                    MessageBox.Show("No hay expedientes registrados en el sistema para exportar.", "Vacío", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                string f_sac = cb_filtro_sac.SelectedItem.ToString();
                string f_fec = txt_filtro_fec.Text.Trim();

                if (f_fec != "")
                {
                    DateTime dtFiltro;
                    if (f_fec.Length != 10 || !DateTime.TryParseExact(f_fec, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out dtFiltro))
                    {
                        MessageBox.Show("La Fecha Oficial ingresada en el filtro no es válida. Corríjala antes de exportar.", "Fecha Inválida", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                }

                List<ItemReporte> filtrados = new List<ItemReporte>();

                for (int i = 0; i < this.db.nombres.Count; i++)
                {
                    if (f_sac != "Todos" && this.db.tipos[i] != f_sac)
                    {
                        continue;
                    }
                    if (f_fec != "" && this.db.fechas_sac[i] != f_fec)
                    {
                        continue;
                    }

                    string nom = this.db.nombres[i];
                    if (this.db.tipos[i] == "Matrimonio Regular")
                    {
                        nom = this.db.nombres[i] + ", " + this.db.nombres_sec[i];
                    }

                    string deuda_val = this.db.deudas[i];
                    string texto_deuda = "No tiene deuda";
                    if (deuda_val != "0" && deuda_val != "")
                    {
                        texto_deuda = "S/ " + deuda_val;
                    }

                    ItemReporte item = new ItemReporte();
                    item.nom = nom;
                    item.tipo = this.db.tipos[i];
                    item.dni = this.db.dnis_prin[i];
                    item.est = this.db.estados[i];
                    item.deuda = texto_deuda;
                    filtrados.Add(item);
                }

                if (filtrados.Count == 0)
                {
                    MessageBox.Show("No hay expedientes que coincidan con los filtros actuales para exportar.", "Vacío", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                SaveFileDialog sfd = new SaveFileDialog();
                sfd.DefaultExt = ".pdf";
                sfd.FileName = "Reporte_General.pdf";
                sfd.Filter = "PDF (*.pdf)|*.pdf";
                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    this.logica.generar_reporte_general_pdf(sfd.FileName, filtrados);
                    MessageBox.Show("Reporte exportado correctamente.", "Éxito", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            };

            this.content_frame.Controls.Add(btnExportar);
        }

        // ================= 8. OPCIONES AVANZADAS: CONSTANCIAS ANTIGUAS (EN EL MISMO CSV) =================
        private void SepararNombreCompleto(string completo, out string nom, out string ape)
        {
            nom = "";
            ape = "";
            if (string.IsNullOrWhiteSpace(completo) || completo == "---")
            {
                return;
            }
            string[] partes = completo.Trim().Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
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
                nom = completo.Trim();
            }
        }

        private void ObtenerNombresHistoricos(int idx, out string n1, out string a1, out string n2, out string a2)
        {
            n1 = "";
            a1 = "";
            n2 = "";
            a2 = "";
            string mem = this.db.checks_mem[idx];
            if (mem.StartsWith("HIST|"))
            {
                string[] partes = mem.Split('|');
                if (partes.Length >= 5)
                {
                    n1 = partes[1];
                    a1 = partes[2];
                    n2 = (partes[3] != "---") ? partes[3] : "";
                    a2 = (partes[4] != "---") ? partes[4] : "";
                    return;
                }
            }
            this.SepararNombreCompleto(this.db.nombres[idx], out n1, out a1);
            this.SepararNombreCompleto(this.db.nombres_sec[idx], out n2, out a2);
        }

        private void mostrar_opciones_avanzadas()
        {
            this.limpiar_contenido();

            Label lblTit = new Label();
            lblTit.Text = "Opciones Avanzadas - Constancias Antiguas";
            lblTit.Font = new Font("Segoe UI", 20f, FontStyle.Bold);
            lblTit.ForeColor = ColorTranslator.FromHtml("#2563eb");
            lblTit.Location = new Point(40, 15);
            lblTit.AutoSize = true;
            this.content_frame.Controls.Add(lblTit);

            Label lblSub = new Label();
            lblSub.Text = "Registro, edición y eliminación de constancias inscritas anteriormente";
            lblSub.Font = new Font("Segoe UI", 10.5f, FontStyle.Regular);
            lblSub.ForeColor = ColorTranslator.FromHtml("#64748b");
            lblSub.Location = new Point(40, 50);
            lblSub.AutoSize = true;
            this.content_frame.Controls.Add(lblSub);

            Panel f_form = new Panel();
            f_form.Location = new Point(40, 80);
            f_form.Size = new Size(920, 275);
            f_form.BackColor = Color.White;
            f_form.BorderStyle = BorderStyle.FixedSingle;

            int idx_hist_seleccionado = -1;
            bool cargandoDesdeTabla = false;

            // Fila 1: Selección de Sacramento
            Label lblSac = new Label() { Text = "Sacramento:", Font = new Font("Segoe UI", 10f, FontStyle.Bold), Location = new Point(25, 20), AutoSize = true };
            ComboBox cb_sac = new ComboBox() { DropDownStyle = ComboBoxStyle.DropDownList, Location = new Point(125, 17), Width = 200 };
            cb_sac.Items.AddRange(new string[] { "Bautizo", "Primera Comunión", "Confirmación", "Matrimonio Regular" });
            f_form.Controls.AddRange(new Control[] { lblSac, cb_sac });

            // Fila 2: DNI, Nombre, Apellido
            Label lblDni1 = new Label() { Text = "DNI:", Font = new Font("Segoe UI", 10f, FontStyle.Bold), Location = new Point(25, 65), Size = new Size(95, 25) };
            TextBox txt_dni1 = new TextBox() { Location = new Point(125, 62), Width = 120 };
            this.adjuntar_validacion(txt_dni1, "dni");

            Label lblNom1 = new Label() { Text = "Nombre:", Font = new Font("Segoe UI", 10f, FontStyle.Bold), Location = new Point(265, 65), Size = new Size(75, 25) };
            TextBox txt_nom1 = new TextBox() { Location = new Point(345, 62), Width = 210 };
            AsignarPlaceholder(txt_nom1, "Nombres");
            this.adjuntar_validacion(txt_nom1, "texto");

            Label lblApe1 = new Label() { Text = "Apellido:", Font = new Font("Segoe UI", 10f, FontStyle.Bold), Location = new Point(575, 65), Size = new Size(75, 25) };
            TextBox txt_ape1 = new TextBox() { Location = new Point(655, 62), Width = 230 };
            AsignarPlaceholder(txt_ape1, "Apellidos");
            this.adjuntar_validacion(txt_ape1, "texto");

            f_form.Controls.AddRange(new Control[] { lblDni1, txt_dni1, lblNom1, txt_nom1, lblApe1, txt_ape1 });

            // Fila 3 (Visible cuando es Matrimonio Regular)
            Label lblDni2 = new Label() { Text = "DNI Novia:", Font = new Font("Segoe UI", 10f, FontStyle.Bold), Location = new Point(25, 108), Size = new Size(95, 25), Visible = false };
            TextBox txt_dni2 = new TextBox() { Location = new Point(125, 105), Width = 120, Visible = false };
            this.adjuntar_validacion(txt_dni2, "dni");

            Label lblNom2 = new Label() { Text = "Nombre:", Font = new Font("Segoe UI", 10f, FontStyle.Bold), Location = new Point(265, 108), Size = new Size(75, 25), Visible = false };
            TextBox txt_nom2 = new TextBox() { Location = new Point(345, 105), Width = 210, Visible = false };
            AsignarPlaceholder(txt_nom2, "Nombres Novia");
            this.adjuntar_validacion(txt_nom2, "texto");

            Label lblApe2 = new Label() { Text = "Apellido:", Font = new Font("Segoe UI", 10f, FontStyle.Bold), Location = new Point(575, 108), Size = new Size(75, 25), Visible = false };
            TextBox txt_ape2 = new TextBox() { Location = new Point(655, 105), Width = 230, Visible = false };
            AsignarPlaceholder(txt_ape2, "Apellidos Novia");
            this.adjuntar_validacion(txt_ape2, "texto");

            f_form.Controls.AddRange(new Control[] { lblDni2, txt_dni2, lblNom2, txt_nom2, lblApe2, txt_ape2 });

            this.VincularAutocompletadoReniecSeparado(txt_dni1, txt_nom1, txt_ape1, txt_dni2, () => cargandoDesdeTabla);
            this.VincularAutocompletadoReniecSeparado(txt_dni2, txt_nom2, txt_ape2, txt_dni1, () => cargandoDesdeTabla);

            // Fila 4: Fecha, Libro, Folio, Asiento
            Label lblFec = new Label() { Text = "Fecha:", Font = new Font("Segoe UI", 10f, FontStyle.Bold), Location = new Point(25, 155), Size = new Size(95, 25) };
            TextBox txt_fec = new TextBox() { Location = new Point(125, 152), Width = 130 };
            AsignarPlaceholder(txt_fec, "DD/MM/AAAA");
            this.adjuntar_validacion(txt_fec, "fecha");

            Label lblLib = new Label() { Text = "Libro:", Font = new Font("Segoe UI", 10f, FontStyle.Bold), Location = new Point(295, 155), AutoSize = true };
            TextBox txt_lib = new TextBox() { Location = new Point(350, 152), Width = 90 };
            AsignarPlaceholder(txt_lib, "---");
            this.adjuntar_validacion(txt_lib, "registro_libro");

            Label lblFol = new Label() { Text = "Folio:", Font = new Font("Segoe UI", 10f, FontStyle.Bold), Location = new Point(475, 155), AutoSize = true };
            TextBox txt_fol = new TextBox() { Location = new Point(530, 152), Width = 90 };
            AsignarPlaceholder(txt_fol, "---");
            this.adjuntar_validacion(txt_fol, "registro_libro");

            Label lblAsi = new Label() { Text = "Asiento:", Font = new Font("Segoe UI", 10f, FontStyle.Bold), Location = new Point(655, 155), AutoSize = true };
            TextBox txt_asi = new TextBox() { Location = new Point(725, 152), Width = 90 };
            AsignarPlaceholder(txt_asi, "---");
            this.adjuntar_validacion(txt_asi, "registro_libro");

            f_form.Controls.AddRange(new Control[] { lblFec, txt_fec, lblLib, txt_lib, lblFol, txt_fol, lblAsi, txt_asi });

            cb_sac.SelectedIndexChanged += (s, e) =>
            {
                bool esMatrimonio = (cb_sac.SelectedItem != null && cb_sac.SelectedItem.ToString() == "Matrimonio Regular");
                lblDni1.Text = esMatrimonio ? "DNI Novio:" : "DNI:";
                lblDni2.Visible = esMatrimonio;
                txt_dni2.Visible = esMatrimonio;
                lblNom2.Visible = esMatrimonio;
                txt_nom2.Visible = esMatrimonio;
                lblApe2.Visible = esMatrimonio;
                txt_ape2.Visible = esMatrimonio;

                if (!esMatrimonio)
                {
                    txt_dni2.Text = "";
                    txt_nom2.Text = "";
                    txt_ape2.Text = "";
                }
                f_form.Invalidate();
            };
            cb_sac.SelectedItem = "Bautizo";

            // Fila 5: Botones (Registrar, Editar, Eliminar, Limpiar)
            Button btnRegistrar = CrearBotonAccion("Registrar", ColorTranslator.FromHtml("#2563eb"), new Point(25, 210), new Size(200, 42), 10.5f);
            Button btnEditar = CrearBotonAccion("Editar / Actualizar", ColorTranslator.FromHtml("#10b981"), new Point(245, 210), new Size(200, 42), 10.5f);
            Button btnEliminar = CrearBotonAccion("Eliminar", ColorTranslator.FromHtml("#ef4444"), new Point(465, 210), new Size(200, 42), 10.5f);
            Button btnLimpiar = CrearBotonAccion("Limpiar Campos", ColorTranslator.FromHtml("#64748b"), new Point(685, 210), new Size(200, 42), 10.5f);

            f_form.Controls.AddRange(new Control[] { btnRegistrar, btnEditar, btnEliminar, btnLimpiar });
            this.content_frame.Controls.Add(f_form);

            // Tabla inferior con las Constancias Antiguas almacenadas en registros_sigep.csv
            DataGridView tablaHist = new DataGridView();
            tablaHist.Location = new Point(40, 370);
            tablaHist.Size = new Size(920, 320);
            tablaHist.BackgroundColor = Color.White;
            tablaHist.BorderStyle = BorderStyle.FixedSingle;
            tablaHist.AllowUserToAddRows = false;
            tablaHist.ReadOnly = true;
            tablaHist.RowHeadersVisible = false;
            tablaHist.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            tablaHist.MultiSelect = false;
            tablaHist.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            tablaHist.DefaultCellStyle.WrapMode = DataGridViewTriState.True;
            tablaHist.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;
            tablaHist.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            tablaHist.DefaultCellStyle.Font = new Font("Segoe UI", 9.5f, FontStyle.Regular);

            tablaHist.Columns.Add("colDni", "DNI");
            tablaHist.Columns.Add("colNom", "Nombre");
            tablaHist.Columns.Add("colApe", "Apellido");
            tablaHist.Columns.Add("colSac", "Sacramento");
            tablaHist.Columns.Add("colFec", "Fecha");
            tablaHist.Columns.Add("colLib", "Libro");
            tablaHist.Columns.Add("colFol", "Folio");
            tablaHist.Columns.Add("colAsi", "Asiento");

            this.content_frame.Controls.Add(tablaHist);

            Action refrescarTablaHist = () =>
            {
                tablaHist.Rows.Clear();
                for (int i = 0; i < this.db.nombres.Count; i++)
                {
                    if (!this.db.checks_mem[i].StartsWith("HIST|"))
                    {
                        continue;
                    }

                    string n1, a1, n2, a2;
                    this.ObtenerNombresHistoricos(i, out n1, out a1, out n2, out a2);

                    string dniMostrar = this.db.dnis_prin[i];
                    string nomMostrar = n1;
                    string apeMostrar = a1;

                    if (this.db.tipos[i] == "Matrimonio Regular" && this.db.dnis_sec[i] != "---" && this.db.dnis_sec[i] != "")
                    {
                        dniMostrar = this.db.dnis_prin[i] + "\n" + this.db.dnis_sec[i];
                        nomMostrar = n1 + "\n" + n2;
                        apeMostrar = a1 + "\n" + a2;
                    }

                    int rowIdx = tablaHist.Rows.Add(
                        dniMostrar,
                        nomMostrar,
                        apeMostrar,
                        this.db.tipos[i],
                        this.db.fechas_sac[i],
                        this.db.libros[i],
                        this.db.folios[i],
                        this.db.asientos[i]
                    );
                    tablaHist.Rows[rowIdx].Tag = i;
                }
            };

            Action limpiarCamposHist = () =>
            {
                cargandoDesdeTabla = true;
                idx_hist_seleccionado = -1;
                cb_sac.SelectedItem = "Bautizo";
                txt_dni1.Text = "";
                txt_nom1.Text = "";
                txt_ape1.Text = "";
                txt_dni2.Text = "";
                txt_nom2.Text = "";
                txt_ape2.Text = "";
                txt_fec.Text = "";
                txt_lib.Text = "";
                txt_fol.Text = "";
                txt_asi.Text = "";
                tablaHist.ClearSelection();
                cargandoDesdeTabla = false;
                f_form.Invalidate();
            };

            Action<int> cargarRegistroEnFormulario = (idx) =>
            {
                if (idx < 0 || idx >= this.db.nombres.Count)
                {
                    return;
                }
                cargandoDesdeTabla = true;
                idx_hist_seleccionado = idx;

                string n1, a1, n2, a2;
                this.ObtenerNombresHistoricos(idx, out n1, out a1, out n2, out a2);

                cb_sac.SelectedItem = this.db.tipos[idx];
                txt_dni1.Text = this.db.dnis_prin[idx];
                txt_nom1.Text = n1;
                txt_ape1.Text = a1;

                if (this.db.tipos[idx] == "Matrimonio Regular")
                {
                    txt_dni2.Text = (this.db.dnis_sec[idx] != "---") ? this.db.dnis_sec[idx] : "";
                    txt_nom2.Text = n2;
                    txt_ape2.Text = a2;
                }
                else
                {
                    txt_dni2.Text = "";
                    txt_nom2.Text = "";
                    txt_ape2.Text = "";
                }

                txt_fec.Text = this.db.fechas_sac[idx];
                txt_lib.Text = (this.db.libros[idx] != "---") ? this.db.libros[idx] : "";
                txt_fol.Text = (this.db.folios[idx] != "---") ? this.db.folios[idx] : "";
                txt_asi.Text = (this.db.asientos[idx] != "---") ? this.db.asientos[idx] : "";
                cargandoDesdeTabla = false;
                f_form.Invalidate();
            };

            tablaHist.CellClick += (s, e) =>
            {
                if (e.RowIndex >= 0 && tablaHist.Rows[e.RowIndex].Tag != null)
                {
                    int idxReal = (int)tablaHist.Rows[e.RowIndex].Tag;
                    cargarRegistroEnFormulario(idxReal);
                }
            };

            btnLimpiar.Click += (s, e) => limpiarCamposHist();

            Func<int, bool> validarFormularioHistorico = (idxIgnorar) =>
            {
                string sac = cb_sac.SelectedItem.ToString();
                string d1 = txt_dni1.Text.Trim();
                string n1 = Regex.Replace(txt_nom1.Text.Trim(), @"\s+", " ");
                string a1 = Regex.Replace(txt_ape1.Text.Trim(), @"\s+", " ");
                string d2 = txt_dni2.Text.Trim();
                string n2 = Regex.Replace(txt_nom2.Text.Trim(), @"\s+", " ");
                string a2 = Regex.Replace(txt_ape2.Text.Trim(), @"\s+", " ");
                string fec = txt_fec.Text.Trim();
                string lib = txt_lib.Text.Trim();
                string fol = txt_fol.Text.Trim();
                string asi = txt_asi.Text.Trim();

                List<string> errores = new List<string>();

                if (d1.Length != 8 || d1 == "00000000")
                {
                    errores.Add("- El DNI debe tener exactamente 8 dígitos numéricos válidos.");
                }
                if (!this.EsNombreValido(n1))
                {
                    errores.Add("- El Nombre es obligatorio (mínimo 2 letras).");
                }
                if (!this.EsNombreValido(a1))
                {
                    errores.Add("- El Apellido es obligatorio (mínimo 2 letras).");
                }

                if (sac == "Matrimonio Regular")
                {
                    if (d2.Length != 8 || d2 == "00000000")
                    {
                        errores.Add("- El DNI de la Novia debe tener 8 dígitos numéricos válidos.");
                    }
                    if (!this.EsNombreValido(n2))
                    {
                        errores.Add("- El Nombre de la Novia es obligatorio.");
                    }
                    if (!this.EsNombreValido(a2))
                    {
                        errores.Add("- El Apellido de la Novia es obligatorio.");
                    }
                    if (d1 != "" && d1 == d2)
                    {
                        errores.Add("- No puede ingresar 2 DNIs iguales. Los DNIs de los esposos deben ser diferentes.");
                    }
                    if (n1 != "" && a1 != "" && (n1 + " " + a1).ToLower() == (n2 + " " + a2).ToLower())
                    {
                        errores.Add("- Los nombres y apellidos de ambos cónyuges no pueden ser idénticos.");
                    }
                }

                DateTime fechaObj;
                if (fec.Length != 10 || !DateTime.TryParseExact(fec, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out fechaObj))
                {
                    errores.Add("- La Fecha debe ser una fecha real en formato DD/MM/AAAA.");
                }
                else
                {
                    if (fechaObj.Date > DateTime.Now.Date)
                    {
                        errores.Add("- Al ser una Constancia Antigua, la fecha en que recibió el sacramento no puede ser futura.");
                    }
                    if (fechaObj.Year < 1900)
                    {
                        errores.Add("- El año de la fecha debe ser válido (mayor o igual a 1900).");
                    }
                }

                if (!this.EsNumeroPositivoValido(lib))
                {
                    errores.Add("- El Libro es obligatorio y solo admite números enteros positivos.");
                }
                if (!this.EsNumeroPositivoValido(fol))
                {
                    errores.Add("- El Folio es obligatorio y solo admite números enteros positivos.");
                }
                if (!this.EsNumeroPositivoValido(asi))
                {
                    errores.Add("- El Asiento es obligatorio y solo admite números enteros positivos.");
                }

                for (int i = 0; i < this.db.dnis_prin.Count; i++)
                {
                    if (i == idxIgnorar)
                    {
                        continue;
                    }
                    if (this.db.tipos[i] == sac)
                    {
                        if (this.db.dnis_prin[i] == d1 || (sac == "Matrimonio Regular" && this.db.dnis_sec[i] == d1))
                        {
                            errores.Add("- El DNI " + d1 + " ya se encuentra registrado en " + sac + ".");
                            break;
                        }
                        if (sac == "Matrimonio Regular" && (this.db.dnis_prin[i] == d2 || this.db.dnis_sec[i] == d2))
                        {
                            errores.Add("- El DNI " + d2 + " ya se encuentra registrado en " + sac + ".");
                            break;
                        }
                    }
                }

                if (errores.Count > 0)
                {
                    MessageBox.Show("Por favor corrija los siguientes errores:\n\n" + string.Join("\n", errores), "Validación de Constancia Antigua", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return false;
                }
                return true;
            };

            btnRegistrar.Click += (s, e) =>
            {
                if (!validarFormularioHistorico(-1))
                {
                    return;
                }

                DialogResult r = MessageBox.Show("¿Está seguro de registrar esta constancia antigua?", "Confirmar Registro", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (r != DialogResult.Yes)
                {
                    return;
                }

                string sac = cb_sac.SelectedItem.ToString();
                string d1 = txt_dni1.Text.Trim();
                string n1 = Regex.Replace(txt_nom1.Text.Trim(), @"\s+", " ").Replace("|", "");
                string a1 = Regex.Replace(txt_ape1.Text.Trim(), @"\s+", " ").Replace("|", "");
                string nomCompleto1 = (n1 + " " + a1).Trim();

                string d2 = "---";
                string n2 = "---";
                string a2 = "---";
                string nomCompleto2 = "---";

                if (sac == "Matrimonio Regular")
                {
                    d2 = txt_dni2.Text.Trim();
                    n2 = Regex.Replace(txt_nom2.Text.Trim(), @"\s+", " ").Replace("|", "");
                    a2 = Regex.Replace(txt_ape2.Text.Trim(), @"\s+", " ").Replace("|", "");
                    nomCompleto2 = (n2 + " " + a2).Trim();
                }

                string marcaHistorica = "HIST|" + n1 + "|" + a1 + "|" + n2 + "|" + a2;

                this.db.registrar(
                    d1,
                    d2,
                    nomCompleto1,
                    nomCompleto2,
                    "",
                    "N/A",
                    sac,
                    "Finalizado",
                    "Constancia histórica registrada.",
                    "",
                    marcaHistorica,
                    txt_fec.Text.Trim(),
                    txt_lib.Text.Trim(),
                    txt_fol.Text.Trim(),
                    txt_asi.Text.Trim(),
                    "Pagado",
                    "0"
                );

                refrescarTablaHist();
                limpiarCamposHist();
                MessageBox.Show("Constancia antigua registrada correctamente en registros_sigep.csv.", "Éxito", MessageBoxButtons.OK, MessageBoxIcon.Information);
            };

            btnEditar.Click += (s, e) =>
            {
                if (idx_hist_seleccionado == -1)
                {
                    MessageBox.Show("Seleccione primero un registro de la tabla inferior haciendo clic sobre él para poder editarlo.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
                if (!validarFormularioHistorico(idx_hist_seleccionado))
                {
                    return;
                }

                DialogResult r = MessageBox.Show("¿Está seguro de guardar los cambios en esta constancia antigua?", "Confirmar Edición", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (r != DialogResult.Yes)
                {
                    return;
                }

                string sac = cb_sac.SelectedItem.ToString();
                string d1 = txt_dni1.Text.Trim();
                string n1 = Regex.Replace(txt_nom1.Text.Trim(), @"\s+", " ").Replace("|", "");
                string a1 = Regex.Replace(txt_ape1.Text.Trim(), @"\s+", " ").Replace("|", "");
                string nomCompleto1 = (n1 + " " + a1).Trim();

                string d2 = "---";
                string n2 = "---";
                string a2 = "---";
                string nomCompleto2 = "---";

                if (sac == "Matrimonio Regular")
                {
                    d2 = txt_dni2.Text.Trim();
                    n2 = Regex.Replace(txt_nom2.Text.Trim(), @"\s+", " ").Replace("|", "");
                    a2 = Regex.Replace(txt_ape2.Text.Trim(), @"\s+", " ").Replace("|", "");
                    nomCompleto2 = (n2 + " " + a2).Trim();
                }

                string marcaHistorica = "HIST|" + n1 + "|" + a1 + "|" + n2 + "|" + a2;
                this.db.tipos[idx_hist_seleccionado] = sac;

                this.db.actualizar_completo(
                    idx_hist_seleccionado,
                    d1,
                    d2,
                    nomCompleto1,
                    nomCompleto2,
                    "",
                    "N/A",
                    "Finalizado",
                    "Constancia histórica registrada.",
                    "",
                    marcaHistorica,
                    txt_fec.Text.Trim(),
                    txt_lib.Text.Trim(),
                    txt_fol.Text.Trim(),
                    txt_asi.Text.Trim(),
                    "Pagado",
                    "0"
                );

                refrescarTablaHist();
                limpiarCamposHist();
                MessageBox.Show("Constancia antigua actualizada correctamente.", "Éxito", MessageBoxButtons.OK, MessageBoxIcon.Information);
            };

            btnEliminar.Click += (s, e) =>
            {
                if (idx_hist_seleccionado == -1)
                {
                    MessageBox.Show("Seleccione primero un registro de la tabla inferior haciendo clic sobre él para poder eliminarlo.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
                DialogResult r = MessageBox.Show("¿Está seguro de eliminar permanentemente esta constancia antigua?", "Confirmar Eliminación", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                if (r == DialogResult.Yes)
                {
                    this.db.eliminar(idx_hist_seleccionado);
                    refrescarTablaHist();
                    limpiarCamposHist();
                    MessageBox.Show("Constancia antigua eliminada correctamente.", "Eliminado", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            };

            refrescarTablaHist();
        }
    }
}