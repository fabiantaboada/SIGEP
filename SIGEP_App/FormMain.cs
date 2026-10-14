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
            this.MinimumSize = new Size(1080, 740);
            this.WindowState = FormWindowState.Maximized;
            this.BackColor = ColorTranslator.FromHtml("#f8fafc");
            this.Font = new Font("Segoe UI", 10f, FontStyle.Regular);

            this.ruta_assets = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "assets");
            if (!Directory.Exists(this.ruta_assets))
            {
                string rutaAlternativa = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\assets"));
                if (Directory.Exists(rutaAlternativa))
                {
                    this.ruta_assets = rutaAlternativa;
                }
            }

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

        private string ObtenerEtiquetaDniPorSacramento(string sacramento)
        {
            if (sacramento == "Bautizo")
            {
                return "DNI Bautizado:";
            }
            else if (sacramento == "Primera Comunión")
            {
                return "DNI Comulgante:";
            }
            else if (sacramento == "Confirmación")
            {
                return "DNI Confirmando:";
            }
            else if (sacramento == "Matrimonio")
            {
                return "DNI Novio:";
            }
            return "DNI Titular:";
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
                    "Este software ha sido desarrollado para la gestión documentaria y archivo sacramental de la Parroquia San José de Bellavista.\n\n" +
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
                entry_widget.MaxLength = 70;
                entry_widget.CharacterCasing = CharacterCasing.Upper;
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
                    string mayusConTildes = val.ToUpper(new CultureInfo("es-PE"));
                    string clean_val = Regex.Replace(mayusConTildes, @"[^A-ZÁÉÍÓÚÑÜ\s\.\-]", "");
                    if (clean_val != val)
                    {
                        int pos = entry_widget.SelectionStart;
                        entry_widget.Text = clean_val;
                        entry_widget.SelectionStart = Math.Min(pos, clean_val.Length);
                        val = clean_val;
                    }
                    string soloLetras = Regex.Replace(val, @"[^A-ZÁÉÍÓÚÑÜ]", "");
                    if (soloLetras.Length >= 2)
                    {
                        this.ActualizarBorde(entry_widget, verde);
                    }
                    else
                    {
                        this.ActualizarBorde(entry_widget, rojo);
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

        private void VincularAutocompletadoReniecSeparado(TextBox txtDniActual, TextBox txtNombre, TextBox txtApellido, TextBox txtOtroDni, Func<bool> estaCargandoDatos)
        {
            string ultimoDniConsultado = "";
            txtDniActual.TextChanged += (s, e) =>
            {
                if (!txtDniActual.Visible || !txtDniActual.Enabled)
                {
                    return;
                }
                if (estaCargandoDatos != null && estaCargandoDatos())
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

                    if (txtOtroDni != null && txtOtroDni.Visible && txtOtroDni.Enabled && txtOtroDni.Text.Trim() == val)
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
                        txtNombre.Text = BaseDatos.A_MayusculasConTildes(nom);
                        txtApellido.Text = BaseDatos.A_MayusculasConTildes(ape);
                    }
                    else
                    {
                        MessageBox.Show("DNI no encontrado en RENIEC. Ingrese manualmente los nombres y apellidos.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Information);
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
            string soloLetras = Regex.Replace(BaseDatos.A_MayusculasConTildes(nombre), @"[^A-ZÁÉÍÓÚÑÜ]", "");
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

        private string[] ObtenerRequisitosSacramento(string sacramento, bool tienePadrinos, string estadoCivilPadrinos)
        {
            List<string> reqs = new List<string>();
            if (sacramento == "Bautizo")
            {
                reqs.Add("1. Copia DNI Bautizado");
                reqs.Add("2. Partida Nacimiento");
                reqs.Add("3. Copia DNI Padre");
                reqs.Add("4. Copia DNI Madre");
                if (tienePadrinos)
                {
                    reqs.Add("5. Copia DNI Padrino / Madrina");
                    if (estadoCivilPadrinos == "Casado(a)")
                    {
                        reqs.Add("6. Partida Matrimonio Religioso Padrinos");
                    }
                    else
                    {
                        reqs.Add("6. Constancia Confirmación Padrino/Madrina");
                    }
                }
            }
            else if (sacramento == "Primera Comunión")
            {
                reqs.Add("1. Copia DNI Comulgante");
                reqs.Add("2. Partida Bautismo");
                reqs.Add("3. Copia DNI Padre");
                reqs.Add("4. Copia DNI Madre");
                if (tienePadrinos)
                {
                    reqs.Add("5. Copia DNI Padrino / Madrina");
                }
            }
            else if (sacramento == "Confirmación")
            {
                reqs.Add("1. Copia DNI Confirmando");
                reqs.Add("2. Partida Bautismo");
                reqs.Add("3. Constancia 1ra Comunión");
                if (tienePadrinos)
                {
                    reqs.Add("4. Copia DNI Padrino / Madrina");
                    if (estadoCivilPadrinos == "Casado(a)")
                    {
                        reqs.Add("5. Partida Matrimonio Religioso Padrinos");
                    }
                    else
                    {
                        reqs.Add("5. Constancia Confirmación Padrino/Madrina");
                    }
                }
            }
            else if (sacramento == "Matrimonio")
            {
                reqs.Add("1. Copia DNI Novio");
                reqs.Add("2. Copia DNI Novia");
                reqs.Add("3. Partida Bautismo Novio");
                reqs.Add("4. Partida Bautismo Novia");
                reqs.Add("5. Confirmación Novio");
                reqs.Add("6. Confirmación Novia");
                reqs.Add("7. Copia DNI Testigo 1");
                reqs.Add("8. Copia DNI Testigo 2");
                reqs.Add("9. Copia DNI Padrinos");
            }
            return reqs.ToArray();
        }

        private void DesmarcarSoloRequisitosPadrino(string sacramento, List<CheckBox> listaChecks, bool desmarcarDniPadrino, bool desmarcarCertificadoEstadoCivil)
        {
            if (sacramento == "Bautizo")
            {
                if (desmarcarDniPadrino && listaChecks.Count > 4)
                {
                    listaChecks[4].Checked = false;
                }
                if (desmarcarCertificadoEstadoCivil && listaChecks.Count > 5)
                {
                    listaChecks[5].Checked = false;
                }
            }
            else if (sacramento == "Primera Comunión")
            {
                if (desmarcarDniPadrino && listaChecks.Count > 4)
                {
                    listaChecks[4].Checked = false;
                }
            }
            else if (sacramento == "Confirmación")
            {
                if (desmarcarDniPadrino && listaChecks.Count > 3)
                {
                    listaChecks[3].Checked = false;
                }
                if (desmarcarCertificadoEstadoCivil && listaChecks.Count > 4)
                {
                    listaChecks[4].Checked = false;
                }
            }
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
            lblTotal.Text = this.db.dnis_prin.Count.ToString();
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
                        MessageBox.Show("La fecha de Primera Comunión no existe en el calendario.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                        MessageBox.Show("La fecha de Confirmación no existe en el calendario.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
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

        // ================= 5. REGISTRO DOCUMENTARIO =================
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
            f_main.Size = new Size(940, 710);
            f_main.BackColor = Color.White;
            f_main.BorderStyle = BorderStyle.FixedSingle;

            Label lblTram = new Label() { Text = "Trámite:", Font = new Font("Segoe UI", 10f, FontStyle.Bold), Location = new Point(30, 20), AutoSize = true };
            ComboBox cb_tipo = new ComboBox() { DropDownStyle = ComboBoxStyle.DropDownList, Location = new Point(165, 17), Width = 220 };
            cb_tipo.Items.AddRange(new string[] { "Bautizo", "Primera Comunión", "Confirmación", "Matrimonio" });

            f_main.Controls.AddRange(new Control[] { lblTram, cb_tipo });

            Label lbl_d1 = new Label() { Text = "DNI Bautizado:", Font = new Font("Segoe UI", 10f, FontStyle.Bold), Location = new Point(30, 62), Size = new Size(132, 25) };
            TextBox txt_d1 = new TextBox() { Location = new Point(165, 59), Width = 110 };
            this.adjuntar_validacion(txt_d1, "dni");

            Label lbl_nom1 = new Label() { Text = "Nombres:", Font = new Font("Segoe UI", 10f, FontStyle.Bold), Location = new Point(290, 62), AutoSize = true };
            TextBox txt_nom1 = new TextBox() { Location = new Point(365, 59), Width = 215 };
            AsignarPlaceholder(txt_nom1, "NOMBRES");
            this.adjuntar_validacion(txt_nom1, "texto");

            Label lbl_ape1 = new Label() { Text = "Apellidos:", Font = new Font("Segoe UI", 10f, FontStyle.Bold), Location = new Point(595, 62), AutoSize = true };
            TextBox txt_ape1 = new TextBox() { Location = new Point(670, 59), Width = 235 };
            AsignarPlaceholder(txt_ape1, "APELLIDOS");
            this.adjuntar_validacion(txt_ape1, "texto");

            f_main.Controls.AddRange(new Control[] { lbl_d1, txt_d1, lbl_nom1, txt_nom1, lbl_ape1, txt_ape1 });

            Label lbl_d2 = new Label() { Text = "DNI Novia:", Font = new Font("Segoe UI", 10f, FontStyle.Bold), Location = new Point(30, 102), Size = new Size(132, 25), Visible = false };
            TextBox txt_d2 = new TextBox() { Location = new Point(165, 99), Width = 110, Visible = false };
            this.adjuntar_validacion(txt_d2, "dni");

            Label lbl_nom2 = new Label() { Text = "Nombres:", Font = new Font("Segoe UI", 10f, FontStyle.Bold), Location = new Point(290, 102), AutoSize = true, Visible = false };
            TextBox txt_nom2 = new TextBox() { Location = new Point(365, 99), Width = 215, Visible = false };
            AsignarPlaceholder(txt_nom2, "NOMBRES NOVIA");
            this.adjuntar_validacion(txt_nom2, "texto");

            Label lbl_ape2 = new Label() { Text = "Apellidos:", Font = new Font("Segoe UI", 10f, FontStyle.Bold), Location = new Point(595, 102), AutoSize = true, Visible = false };
            TextBox txt_ape2 = new TextBox() { Location = new Point(670, 99), Width = 235, Visible = false };
            AsignarPlaceholder(txt_ape2, "APELLIDOS NOVIA");
            this.adjuntar_validacion(txt_ape2, "texto");

            f_main.Controls.AddRange(new Control[] { lbl_d2, txt_d2, lbl_nom2, txt_nom2, lbl_ape2, txt_ape2 });

            this.VincularAutocompletadoReniecSeparado(txt_d1, txt_nom1, txt_ape1, txt_d2, null);
            this.VincularAutocompletadoReniecSeparado(txt_d2, txt_nom2, txt_ape2, txt_d1, null);

            CheckBox chk_padrinos = new CheckBox() { Text = "¿Cuenta con Padrinos?", Font = new Font("Segoe UI", 10f, FontStyle.Bold), Location = new Point(30, 142), AutoSize = true, Checked = true };
            Label lbl_est_pad = new Label() { Text = "Estado Civil Padrino(s):", Font = new Font("Segoe UI", 10f, FontStyle.Bold), Location = new Point(260, 144), AutoSize = true };
            ComboBox cb_est_pad = new ComboBox() { DropDownStyle = ComboBoxStyle.DropDownList, Location = new Point(435, 141), Width = 160 };
            cb_est_pad.Items.AddRange(new string[] { "Soltero(a)", "Casado(a)" });
            cb_est_pad.SelectedIndex = 0;

            f_main.Controls.AddRange(new Control[] { chk_padrinos, lbl_est_pad, cb_est_pad });

            Panel f_req = new Panel();
            f_req.Location = new Point(30, 180);
            f_req.Size = new Size(875, 185);
            f_req.BackColor = ColorTranslator.FromHtml("#f8fafc");
            f_req.BorderStyle = BorderStyle.FixedSingle;

            Label lblReqTit = new Label() { Text = "Requisitos de Documentación:", Font = new Font("Segoe UI", 10f, FontStyle.Bold), Location = new Point(15, 10), AutoSize = true };
            f_req.Controls.Add(lblReqTit);

            this.chks.Clear();
            for (int i = 0; i < 9; i++)
            {
                CheckBox chk = new CheckBox();
                chk.AutoSize = true;
                int col = i % 2;
                int fila = i / 2;
                chk.Location = new Point(20 + (col * 420), 38 + (fila * 27));
                this.chks.Add(chk);
                f_req.Controls.Add(chk);
            }
            f_main.Controls.Add(f_req);

            Label lblPago = new Label() { Text = "Estado de Pago:", Font = new Font("Segoe UI", 10f, FontStyle.Bold), Location = new Point(30, 380), Size = new Size(125, 25) };
            ComboBox cb_pago = new ComboBox() { DropDownStyle = ComboBoxStyle.DropDownList, Location = new Point(160, 378), Width = 150 };
            cb_pago.Items.AddRange(new string[] { "Pagado", "Falta Pagar" });
            cb_pago.SelectedIndex = 0;

            TextBox txt_deuda = new TextBox() { Location = new Point(330, 378), Width = 150, Visible = false };
            AsignarPlaceholder(txt_deuda, "Monto deuda S/");
            this.adjuntar_validacion(txt_deuda, "numero");

            f_main.Controls.AddRange(new Control[] { lblPago, cb_pago, txt_deuda });

            Panel f_obs_izq = new Panel() { Location = new Point(30, 420), Size = new Size(425, 175), BackColor = ColorTranslator.FromHtml("#f1f5f9"), BorderStyle = BorderStyle.FixedSingle };
            Label lblObsGen = new Label() { Text = "Observación Generada:", Font = new Font("Segoe UI", 10f, FontStyle.Bold), Location = new Point(15, 10), AutoSize = true };
            TextBox txt_obs_auto = new TextBox() { Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical, Location = new Point(15, 36), Size = new Size(395, 122), BackColor = Color.White };
            f_obs_izq.Controls.AddRange(new Control[] { lblObsGen, txt_obs_auto });
            f_main.Controls.Add(f_obs_izq);

            Panel f_obs_der = new Panel() { Location = new Point(480, 420), Size = new Size(425, 175), BackColor = ColorTranslator.FromHtml("#f1f5f9"), BorderStyle = BorderStyle.FixedSingle };
            CheckBox chk_adic = new CheckBox() { Text = "¿Añadir observación adicional manual?", Font = new Font("Segoe UI", 10f, FontStyle.Bold), Location = new Point(15, 10), AutoSize = true };
            TextBox txt_obs_adic = new TextBox() { Multiline = true, ScrollBars = ScrollBars.Vertical, Location = new Point(15, 36), Size = new Size(395, 122), Visible = false };
            f_obs_der.Controls.AddRange(new Control[] { chk_adic, txt_obs_adic });
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

            for (int i = 0; i < 9; i++)
            {
                this.chks[i].CheckedChanged += (s, e) => actualizar_obs_auto();
            }

            Action recargar_requisitos_dinamicos = () =>
            {
                if (cb_tipo.SelectedItem == null)
                {
                    return;
                }
                string eleccion = cb_tipo.SelectedItem.ToString();
                bool tienePad = chk_padrinos.Checked;
                string estPad = cb_est_pad.SelectedItem != null ? cb_est_pad.SelectedItem.ToString() : "Soltero(a)";

                lbl_d1.Text = this.ObtenerEtiquetaDniPorSacramento(eleccion);

                if (eleccion == "Bautizo")
                {
                    lbl_d2.Visible = false;
                    txt_d2.Visible = false;
                    lbl_nom2.Visible = false;
                    txt_nom2.Visible = false;
                    lbl_ape2.Visible = false;
                    txt_ape2.Visible = false;
                    chk_padrinos.Visible = true;
                    lbl_est_pad.Visible = tienePad;
                    cb_est_pad.Visible = tienePad;
                }
                else if (eleccion == "Primera Comunión")
                {
                    lbl_d2.Visible = false;
                    txt_d2.Visible = false;
                    lbl_nom2.Visible = false;
                    txt_nom2.Visible = false;
                    lbl_ape2.Visible = false;
                    txt_ape2.Visible = false;
                    chk_padrinos.Visible = true;
                    lbl_est_pad.Visible = false;
                    cb_est_pad.Visible = false;
                }
                else if (eleccion == "Confirmación")
                {
                    lbl_d2.Visible = false;
                    txt_d2.Visible = false;
                    lbl_nom2.Visible = false;
                    txt_nom2.Visible = false;
                    lbl_ape2.Visible = false;
                    txt_ape2.Visible = false;
                    chk_padrinos.Visible = true;
                    lbl_est_pad.Visible = tienePad;
                    cb_est_pad.Visible = tienePad;
                }
                else if (eleccion == "Matrimonio")
                {
                    lbl_d2.Visible = true;
                    txt_d2.Visible = true;
                    lbl_nom2.Visible = true;
                    txt_nom2.Visible = true;
                    lbl_ape2.Visible = true;
                    txt_ape2.Visible = true;
                    chk_padrinos.Visible = false;
                    lbl_est_pad.Visible = false;
                    cb_est_pad.Visible = false;
                }

                string[] reqs = this.ObtenerRequisitosSacramento(eleccion, tienePad, estPad);
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
                f_main.Invalidate();
                actualizar_obs_auto();
            };

            cb_tipo.SelectedIndexChanged += (s, e) =>
            {
                for (int j = 0; j < this.chks.Count; j++)
                {
                    this.chks[j].Checked = false;
                }
                recargar_requisitos_dinamicos();
            };

            chk_padrinos.CheckedChanged += (s, e) =>
            {
                if (cb_tipo.SelectedItem != null)
                {
                    this.DesmarcarSoloRequisitosPadrino(cb_tipo.SelectedItem.ToString(), this.chks, true, true);
                }
                recargar_requisitos_dinamicos();
            };

            cb_est_pad.SelectedIndexChanged += (s, e) =>
            {
                if (cb_tipo.SelectedItem != null)
                {
                    this.DesmarcarSoloRequisitosPadrino(cb_tipo.SelectedItem.ToString(), this.chks, false, true);
                }
                recargar_requisitos_dinamicos();
            };

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

            cb_tipo.SelectedItem = "Bautizo";

            Button btnGuardarExp = CrearBotonAccion("Inscribir y Guardar Expediente", ColorTranslator.FromHtml("#2563eb"), new Point(295, 620), new Size(350, 50), 13f);

            btnGuardarExp.Click += (s, e) =>
            {
                string tipo_sel = cb_tipo.SelectedItem.ToString();
                string dni1_val = txt_d1.Text.Trim();
                string nom1_val = Regex.Replace(BaseDatos.A_MayusculasConTildes(txt_nom1.Text), @"\s+", " ");
                string ape1_val = Regex.Replace(BaseDatos.A_MayusculasConTildes(txt_ape1.Text), @"\s+", " ");

                string dni2_val = "---";
                string nom2_val = "---";
                string ape2_val = "---";

                if (dni1_val == "" && nom1_val == "" && ape1_val == "")
                {
                    MessageBox.Show("No se han llenado los datos del feligrés.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                List<string> errores = new List<string>();
                if (dni1_val.Length != 8 || dni1_val == "00000000")
                {
                    errores.Add("- El " + this.ObtenerEtiquetaDniPorSacramento(tipo_sel).Replace(":", "") + " debe tener 8 dígitos numéricos válidos.");
                }
                if (!this.EsNombreValido(nom1_val))
                {
                    errores.Add("- Los Nombres del titular deben contener al menos 2 letras válidas.");
                }
                if (!this.EsNombreValido(ape1_val))
                {
                    errores.Add("- Los Apellidos del titular deben contener al menos 2 letras válidas.");
                }

                if (tipo_sel == "Matrimonio")
                {
                    dni2_val = txt_d2.Text.Trim();
                    nom2_val = Regex.Replace(BaseDatos.A_MayusculasConTildes(txt_nom2.Text), @"\s+", " ");
                    ape2_val = Regex.Replace(BaseDatos.A_MayusculasConTildes(txt_ape2.Text), @"\s+", " ");

                    if (dni2_val.Length != 8 || dni2_val == "00000000")
                    {
                        errores.Add("- El DNI Novia debe tener 8 dígitos numéricos válidos.");
                    }
                    if (!this.EsNombreValido(nom2_val))
                    {
                        errores.Add("- Los Nombres de la Novia deben contener al menos 2 letras válidas.");
                    }
                    if (!this.EsNombreValido(ape2_val))
                    {
                        errores.Add("- Los Apellidos de la Novia deben contener al menos 2 letras válidas.");
                    }
                    if (dni1_val != "" && dni1_val == dni2_val)
                    {
                        errores.Add("- No se pueden colocar 2 DNIs iguales. Los DNIs de los novios deben ser diferentes.");
                    }
                    if (nom1_val != "" && ape1_val != "" && (nom1_val + " " + ape1_val) == (nom2_val + " " + ape2_val))
                    {
                        errores.Add("- Los nombres y apellidos de los novios no pueden ser iguales.");
                    }
                }

                for (int idxExist = 0; idxExist < this.db.dnis_prin.Count; idxExist++)
                {
                    if (this.db.tipos[idxExist] == tipo_sel)
                    {
                        if (this.db.dnis_prin[idxExist] == dni1_val || (tipo_sel == "Matrimonio" && this.db.dnis_sec[idxExist] == dni1_val))
                        {
                            errores.Add("- El DNI " + dni1_val + " ya se encuentra registrado en " + tipo_sel + ".");
                            break;
                        }
                        if (tipo_sel == "Matrimonio" && (this.db.dnis_prin[idxExist] == dni2_val || this.db.dnis_sec[idxExist] == dni2_val))
                        {
                            errores.Add("- El DNI " + dni2_val + " ya se encuentra registrado en " + tipo_sel + ".");
                            break;
                        }
                    }
                }

                if (tipo_sel == "Primera Comunión" && this.config.fecha_comunion == "--/--")
                {
                    errores.Add("- Debe configurar la fecha de inicio de Catequesis en 'Configuración de Fechas' antes de registrar.");
                }
                if (tipo_sel == "Confirmación" && this.config.fecha_confirmacion == "--/--")
                {
                    errores.Add("- Debe configurar la fecha de inicio de Catequesis en 'Configuración de Fechas' antes de registrar.");
                }

                string pago = cb_pago.SelectedItem.ToString();
                string deuda = "0";
                if (pago == "Falta Pagar")
                {
                    deuda = txt_deuda.Text.Trim();
                    double monto_float = 0;
                    if (deuda == "" || !double.TryParse(deuda, NumberStyles.Any, CultureInfo.InvariantCulture, out monto_float) || monto_float <= 0)
                    {
                        errores.Add("- Debe especificar un monto de deuda válido mayor a 0.");
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

                string est = (completos && pago == "Pagado") ? "Inscrito(a)" : "Observado";
                string t_pad_str = (tipo_sel == "Matrimonio") ? "SI" : (chk_padrinos.Checked ? "SI" : "NO");
                string est_pad_str = ((tipo_sel == "Bautizo" || tipo_sel == "Confirmación") && chk_padrinos.Checked) ? cb_est_pad.SelectedItem.ToString() : "---";

                DialogResult res_conf = MessageBox.Show("¿Está seguro de inscribir y guardar este expediente?", "Confirmar", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (res_conf != DialogResult.Yes)
                {
                    return;
                }

                List<string> lista_chks = new List<string>();
                for (int k = 0; k < this.reqs_activos; k++)
                {
                    lista_chks.Add(this.chks[k].Checked ? "1" : "0");
                }
                string chks_str = string.Join("-", lista_chks);

                this.db.registrar(
                    "TRAMITE",
                    dni1_val,
                    nom1_val,
                    ape1_val,
                    dni2_val,
                    nom2_val,
                    ape2_val,
                    "N/A",
                    tipo_sel,
                    est,
                    t_pad_str,
                    est_pad_str,
                    chks_str,
                    txt_obs_auto.Text.Trim(),
                    obs_adic_txt,
                    "Por Programar",
                    "---",
                    "---",
                    "---",
                    pago,
                    deuda
                );

                MessageBox.Show("Expediente registrado y guardado correctamente en el sistema.", "Éxito", MessageBoxButtons.OK, MessageBoxIcon.Information);
                this.mostrar_dashboard();
            };

            f_main.Controls.Add(btnGuardarExp);
            this.content_frame.Controls.Add(f_main);
        }

        // ================= 6. BUSCAR Y EDITAR (POR DNI O APELLIDOS) =================
        private void mostrar_buscar()
        {
            this.limpiar_contenido();

            Label lblTit = new Label();
            lblTit.Text = "Buscador y Editor de Expedientes";
            lblTit.Font = new Font("Segoe UI", 20f, FontStyle.Bold);
            lblTit.ForeColor = ColorTranslator.FromHtml("#2563eb");
            lblTit.Location = new Point(40, 15);
            lblTit.AutoSize = true;
            this.content_frame.Controls.Add(lblTit);

            Panel f_top = new Panel();
            f_top.Location = new Point(40, 55);
            f_top.Size = new Size(940, 62);
            f_top.BackColor = Color.White;
            f_top.BorderStyle = BorderStyle.FixedSingle;

            Label lblCriterio = new Label() { Text = "Buscar por:", Font = new Font("Segoe UI", 10f, FontStyle.Bold), Location = new Point(15, 20), AutoSize = true };
            ComboBox cb_criterio = new ComboBox() { DropDownStyle = ComboBoxStyle.DropDownList, Location = new Point(105, 17), Width = 130 };
            cb_criterio.Items.AddRange(new string[] { "DNI", "Apellidos" });
            cb_criterio.SelectedIndex = 0;

            TextBox txt_bus_dni = new TextBox() { Location = new Point(250, 17), Width = 220 };
            AsignarPlaceholder(txt_bus_dni, "Ingrese 8 dígitos del DNI");
            this.adjuntar_validacion(txt_bus_dni, "dni");

            TextBox txt_bus_ape = new TextBox() { Location = new Point(250, 17), Width = 260, Visible = false };
            AsignarPlaceholder(txt_bus_ape, "INGRESE APELLIDOS A BUSCAR");
            this.adjuntar_validacion(txt_bus_ape, "texto");

            cb_criterio.SelectedIndexChanged += (s, e) =>
            {
                bool esDni = (cb_criterio.SelectedItem.ToString() == "DNI");
                txt_bus_dni.Visible = esDni;
                txt_bus_ape.Visible = !esDni;
                txt_bus_dni.Text = "";
                txt_bus_ape.Text = "";
                f_top.Invalidate();
            };

            Button btnConsultar = CrearBotonAccion("Consultar", ColorTranslator.FromHtml("#10b981"), new Point(530, 13), new Size(140, 35));

            f_top.Controls.AddRange(new Control[] { lblCriterio, cb_criterio, txt_bus_dni, txt_bus_ape, btnConsultar });
            this.content_frame.Controls.Add(f_top);

            DataGridView tablaResultados = new DataGridView();
            tablaResultados.Location = new Point(40, 125);
            tablaResultados.Size = new Size(940, 135);
            tablaResultados.BackgroundColor = Color.White;
            tablaResultados.BorderStyle = BorderStyle.FixedSingle;
            tablaResultados.AllowUserToAddRows = false;
            tablaResultados.ReadOnly = true;
            tablaResultados.RowHeadersVisible = false;
            tablaResultados.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            tablaResultados.MultiSelect = false;
            tablaResultados.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            tablaResultados.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            tablaResultados.DefaultCellStyle.Font = new Font("Segoe UI", 9.5f, FontStyle.Regular);

            tablaResultados.Columns.Add("colDni", "DNI");
            tablaResultados.Columns.Add("colNom", "Nombres");
            tablaResultados.Columns.Add("colApe", "Apellidos");
            tablaResultados.Columns.Add("colSac", "Sacramento");
            tablaResultados.Columns.Add("colEst", "Estado");
            tablaResultados.Columns.Add("colPag", "Pago");

            this.content_frame.Controls.Add(tablaResultados);

            Panel f_edit = new Panel();
            f_edit.Location = new Point(40, 270);
            f_edit.Size = new Size(940, 660);
            f_edit.BackColor = Color.White;
            f_edit.BorderStyle = BorderStyle.FixedSingle;
            f_edit.Visible = false;

            int idx_actual = -1;
            bool cargandoEdicion = false;
            bool llenandoTabla = false;

            Label lbl_e_dni1 = new Label() { Text = "DNI Bautizado:", Font = new Font("Segoe UI", 10f, FontStyle.Bold), Location = new Point(20, 18), Size = new Size(132, 25) };
            TextBox t_dni1 = new TextBox() { Location = new Point(155, 15), Width = 110 };
            this.adjuntar_validacion(t_dni1, "dni");

            Label lbl_e_nom1 = new Label() { Text = "Nombres:", Font = new Font("Segoe UI", 10f, FontStyle.Bold), Location = new Point(280, 18), AutoSize = true };
            TextBox t_nom1 = new TextBox() { Location = new Point(355, 15), Width = 215 };
            this.adjuntar_validacion(t_nom1, "texto");

            Label lbl_e_ape1 = new Label() { Text = "Apellidos:", Font = new Font("Segoe UI", 10f, FontStyle.Bold), Location = new Point(585, 18), AutoSize = true };
            TextBox t_ape1 = new TextBox() { Location = new Point(665, 15), Width = 245 };
            this.adjuntar_validacion(t_ape1, "texto");

            f_edit.Controls.AddRange(new Control[] { lbl_e_dni1, t_dni1, lbl_e_nom1, t_nom1, lbl_e_ape1, t_ape1 });

            Label lbl_e_dni2 = new Label() { Text = "DNI Novia:", Font = new Font("Segoe UI", 10f, FontStyle.Bold), Location = new Point(20, 56), Size = new Size(132, 25), Visible = false };
            TextBox t_dni2 = new TextBox() { Location = new Point(155, 53), Width = 110, Visible = false };
            this.adjuntar_validacion(t_dni2, "dni");

            Label lbl_e_nom2 = new Label() { Text = "Nombres:", Font = new Font("Segoe UI", 10f, FontStyle.Bold), Location = new Point(280, 56), AutoSize = true, Visible = false };
            TextBox t_nom2 = new TextBox() { Location = new Point(355, 53), Width = 215, Visible = false };
            this.adjuntar_validacion(t_nom2, "texto");

            Label lbl_e_ape2 = new Label() { Text = "Apellidos:", Font = new Font("Segoe UI", 10f, FontStyle.Bold), Location = new Point(585, 56), AutoSize = true, Visible = false };
            TextBox t_ape2 = new TextBox() { Location = new Point(665, 53), Width = 245, Visible = false };
            this.adjuntar_validacion(t_ape2, "texto");

            f_edit.Controls.AddRange(new Control[] { lbl_e_dni2, t_dni2, lbl_e_nom2, t_nom2, lbl_e_ape2, t_ape2 });

            this.VincularAutocompletadoReniecSeparado(t_dni1, t_nom1, t_ape1, t_dni2, () => cargandoEdicion);
            this.VincularAutocompletadoReniecSeparado(t_dni2, t_nom2, t_ape2, t_dni1, () => cargandoEdicion);

            Label lbl_e_est = new Label() { Text = "Estado:", Font = new Font("Segoe UI", 10f, FontStyle.Bold), Location = new Point(20, 96), AutoSize = true };
            ComboBox c_est = new ComboBox() { DropDownStyle = ComboBoxStyle.DropDownList, Location = new Point(85, 93), Width = 135 };
            c_est.Items.AddRange(new string[] { "Inscrito(a)", "Observado", "Finalizado" });

            Label lbl_e_pag = new Label() { Text = "Pago:", Font = new Font("Segoe UI", 10f, FontStyle.Bold), Location = new Point(235, 96), AutoSize = true };
            ComboBox c_pag = new ComboBox() { DropDownStyle = ComboBoxStyle.DropDownList, Location = new Point(285, 93), Width = 120 };
            c_pag.Items.AddRange(new string[] { "Pagado", "Falta Pagar" });

            TextBox t_deu = new TextBox() { Location = new Point(415, 93), Width = 85 };
            AsignarPlaceholder(t_deu, "Deuda S/");
            this.adjuntar_validacion(t_deu, "numero");

            CheckBox chk_e_padrinos = new CheckBox() { Text = "¿Con Padrinos?", Font = new Font("Segoe UI", 9.5f, FontStyle.Bold), Location = new Point(520, 95), AutoSize = true };
            Label lbl_e_est_pad = new Label() { Text = "Est. Civil:", Font = new Font("Segoe UI", 9.5f, FontStyle.Bold), Location = new Point(665, 96), AutoSize = true };
            ComboBox cb_e_est_pad = new ComboBox() { DropDownStyle = ComboBoxStyle.DropDownList, Location = new Point(740, 93), Width = 140 };
            cb_e_est_pad.Items.AddRange(new string[] { "Soltero(a)", "Casado(a)" });
            cb_e_est_pad.SelectedIndex = 0;

            f_edit.Controls.AddRange(new Control[] { lbl_e_est, c_est, lbl_e_pag, c_pag, t_deu, chk_e_padrinos, lbl_e_est_pad, cb_e_est_pad });

            Panel f_req_edit = new Panel();
            f_req_edit.Location = new Point(20, 135);
            f_req_edit.Size = new Size(890, 180);
            f_req_edit.BackColor = ColorTranslator.FromHtml("#f8fafc");
            f_req_edit.BorderStyle = BorderStyle.FixedSingle;

            Label lblReqEditTit = new Label() { Text = "Requisitos de Documentación:", Font = new Font("Segoe UI", 10f, FontStyle.Bold), Location = new Point(15, 8), AutoSize = true };
            f_req_edit.Controls.Add(lblReqEditTit);

            this.chks_edit.Clear();
            for (int i = 0; i < 9; i++)
            {
                CheckBox chk = new CheckBox();
                chk.AutoSize = true;
                int col = i % 2;
                int fila = i / 2;
                chk.Location = new Point(20 + (col * 430), 34 + (fila * 27));
                this.chks_edit.Add(chk);
                f_req_edit.Controls.Add(chk);
            }
            f_edit.Controls.Add(f_req_edit);

            Panel f_obs_izq_edit = new Panel() { Location = new Point(20, 325), Size = new Size(435, 160), BackColor = ColorTranslator.FromHtml("#f1f5f9"), BorderStyle = BorderStyle.FixedSingle };
            Label lblObsGenE = new Label() { Text = "Observación Generada:", Font = new Font("Segoe UI", 10f, FontStyle.Bold), Location = new Point(15, 8), AutoSize = true };
            TextBox txt_obs_auto_edit = new TextBox() { Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical, Location = new Point(15, 32), Size = new Size(405, 112), BackColor = Color.White };
            f_obs_izq_edit.Controls.AddRange(new Control[] { lblObsGenE, txt_obs_auto_edit });
            f_edit.Controls.Add(f_obs_izq_edit);

            Panel f_obs_der_edit = new Panel() { Location = new Point(475, 325), Size = new Size(435, 160), BackColor = ColorTranslator.FromHtml("#f1f5f9"), BorderStyle = BorderStyle.FixedSingle };
            CheckBox chk_adic_edit = new CheckBox() { Text = "¿Añadir/Ver observación adicional manual?", Font = new Font("Segoe UI", 10f, FontStyle.Bold), Location = new Point(15, 8), AutoSize = true };
            TextBox txt_obs_adic_edit = new TextBox() { Multiline = true, ScrollBars = ScrollBars.Vertical, Location = new Point(15, 32), Size = new Size(405, 112), Visible = false };
            f_obs_der_edit.Controls.AddRange(new Control[] { chk_adic_edit, txt_obs_adic_edit });
            f_edit.Controls.Add(f_obs_der_edit);

            Label lblFecOf = new Label() { Text = "Fecha Oficial:", Font = new Font("Segoe UI", 10f, FontStyle.Bold), Location = new Point(20, 505), Size = new Size(105, 25) };
            TextBox t_fec = new TextBox() { Location = new Point(130, 502), Width = 140 };
            AsignarPlaceholder(t_fec, "DD/MM/AAAA");
            this.adjuntar_validacion(t_fec, "fecha");

            Label lblLib = new Label() { Text = "Libro:", Font = new Font("Segoe UI", 10f, FontStyle.Bold), Location = new Point(300, 505), AutoSize = true };
            TextBox t_l = new TextBox() { Location = new Point(355, 502), Width = 75 };
            AsignarPlaceholder(t_l, "---");
            this.adjuntar_validacion(t_l, "registro_libro");

            Label lblFol = new Label() { Text = "Folio:", Font = new Font("Segoe UI", 10f, FontStyle.Bold), Location = new Point(460, 505), AutoSize = true };
            TextBox t_f = new TextBox() { Location = new Point(515, 502), Width = 75 };
            AsignarPlaceholder(t_f, "---");
            this.adjuntar_validacion(t_f, "registro_libro");

            Label lblAsi = new Label() { Text = "Asiento:", Font = new Font("Segoe UI", 10f, FontStyle.Bold), Location = new Point(620, 505), AutoSize = true };
            TextBox t_a = new TextBox() { Location = new Point(690, 502), Width = 75 };
            AsignarPlaceholder(t_a, "---");
            this.adjuntar_validacion(t_a, "registro_libro");

            f_edit.Controls.AddRange(new Control[] { lblFecOf, t_fec, lblLib, t_l, lblFol, t_f, lblAsi, t_a });

            Button btn_act = CrearBotonAccion("Actualizar Datos", ColorTranslator.FromHtml("#2563eb"), new Point(250, 560), new Size(210, 44), 11f);
            Button btn_eli = CrearBotonAccion("Eliminar Expediente", ColorTranslator.FromHtml("#ef4444"), new Point(485, 560), new Size(210, 44), 11f);

            f_edit.Controls.AddRange(new Control[] { btn_act, btn_eli });
            this.content_frame.Controls.Add(f_edit);

            Action actualizar_obs_auto_edit = () =>
            {
                if (cargandoEdicion)
                {
                    return;
                }
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

            Action reconstruir_requisitos_edicion = () =>
            {
                if (idx_actual < 0 || idx_actual >= this.db.tipos.Count)
                {
                    return;
                }
                string tipo = this.db.tipos[idx_actual];
                bool tienePad = chk_e_padrinos.Checked;
                string estPad = cb_e_est_pad.SelectedItem != null ? cb_e_est_pad.SelectedItem.ToString() : "Soltero(a)";

                chk_e_padrinos.Visible = (tipo != "Matrimonio");
                lbl_e_est_pad.Visible = ((tipo == "Bautizo" || tipo == "Confirmación") && tienePad);
                cb_e_est_pad.Visible = ((tipo == "Bautizo" || tipo == "Confirmación") && tienePad);

                string[] reqs = this.ObtenerRequisitosSacramento(tipo, tienePad, estPad);
                this.reqs_activos_edit = reqs.Length;
                for (int k = 0; k < this.chks_edit.Count; k++)
                {
                    if (k < reqs.Length)
                    {
                        this.chks_edit[k].Text = reqs[k];
                        this.chks_edit[k].Visible = true;
                    }
                    else
                    {
                        this.chks_edit[k].Visible = false;
                        this.chks_edit[k].Checked = false;
                    }
                }
                actualizar_obs_auto_edit();
            };

            for (int i = 0; i < 9; i++)
            {
                this.chks_edit[i].CheckedChanged += (s, e) => actualizar_obs_auto_edit();
            }

            chk_e_padrinos.CheckedChanged += (s, e) =>
            {
                if (!cargandoEdicion && idx_actual >= 0 && idx_actual < this.db.tipos.Count)
                {
                    this.DesmarcarSoloRequisitosPadrino(this.db.tipos[idx_actual], this.chks_edit, true, true);
                    reconstruir_requisitos_edicion();
                }
            };

            cb_e_est_pad.SelectedIndexChanged += (s, e) =>
            {
                if (!cargandoEdicion && idx_actual >= 0 && idx_actual < this.db.tipos.Count)
                {
                    this.DesmarcarSoloRequisitosPadrino(this.db.tipos[idx_actual], this.chks_edit, false, true);
                    reconstruir_requisitos_edicion();
                }
            };

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

            Action<int> cargarExpedienteEnEditor = (idx) =>
            {
                if (idx < 0 || idx >= this.db.dnis_prin.Count)
                {
                    f_edit.Visible = false;
                    return;
                }

                cargandoEdicion = true;
                idx_actual = idx;
                f_edit.Visible = true;

                string tipo = this.db.tipos[idx];
                bool esMatrimonio = (tipo == "Matrimonio");

                lbl_e_dni1.Text = this.ObtenerEtiquetaDniPorSacramento(tipo);

                lbl_e_dni2.Visible = esMatrimonio;
                t_dni2.Visible = esMatrimonio;
                lbl_e_nom2.Visible = esMatrimonio;
                t_nom2.Visible = esMatrimonio;
                lbl_e_ape2.Visible = esMatrimonio;
                t_ape2.Visible = esMatrimonio;

                t_dni1.Text = this.db.dnis_prin[idx];
                t_nom1.Text = BaseDatos.A_MayusculasConTildes(this.db.nombres[idx]);
                t_ape1.Text = BaseDatos.A_MayusculasConTildes(this.db.apellidos[idx]);

                if (esMatrimonio)
                {
                    t_dni2.Text = (this.db.dnis_sec[idx] != "---") ? this.db.dnis_sec[idx] : "";
                    t_nom2.Text = (this.db.nombres_sec[idx] != "---") ? BaseDatos.A_MayusculasConTildes(this.db.nombres_sec[idx]) : "";
                    t_ape2.Text = (this.db.apellidos_sec[idx] != "---") ? BaseDatos.A_MayusculasConTildes(this.db.apellidos_sec[idx]) : "";
                }
                else
                {
                    t_dni2.Text = "";
                    t_nom2.Text = "";
                    t_ape2.Text = "";
                }

                chk_e_padrinos.Checked = (this.db.tiene_padrinos[idx] == "SI");
                if (this.db.estado_padrinos[idx] == "Casado(a)")
                {
                    cb_e_est_pad.SelectedItem = "Casado(a)";
                }
                else
                {
                    cb_e_est_pad.SelectedItem = "Soltero(a)";
                }

                reconstruir_requisitos_edicion();

                string[] memoria = this.db.checks_mem[idx].Split(new char[] { '-', ',' }, StringSplitOptions.RemoveEmptyEntries);
                for (int k = 0; k < this.reqs_activos_edit; k++)
                {
                    if (k < memoria.Length && memoria[k] == "1")
                    {
                        this.chks_edit[k].Checked = true;
                    }
                    else
                    {
                        this.chks_edit[k].Checked = false;
                    }
                }

                if (c_est.Items.Contains(this.db.estados[idx]))
                {
                    c_est.SelectedItem = this.db.estados[idx];
                }
                else
                {
                    c_est.SelectedIndex = 0;
                }

                if (c_pag.Items.Contains(this.db.pagos[idx]))
                {
                    c_pag.SelectedItem = this.db.pagos[idx];
                }
                else
                {
                    c_pag.SelectedIndex = 0;
                }

                if (this.db.pagos[idx] == "Pagado")
                {
                    t_deu.Visible = false;
                    t_deu.Text = "";
                }
                else
                {
                    t_deu.Visible = true;
                    t_deu.Text = this.db.deudas[idx];
                }

                if (!string.IsNullOrEmpty(this.db.obs_adic[idx]))
                {
                    chk_adic_edit.Checked = true;
                    txt_obs_adic_edit.Visible = true;
                    txt_obs_adic_edit.Text = this.db.obs_adic[idx];
                }
                else
                {
                    chk_adic_edit.Checked = false;
                    txt_obs_adic_edit.Visible = false;
                    txt_obs_adic_edit.Text = "";
                }

                t_fec.Text = (this.db.fechas_sac[idx] != "Por Programar") ? this.db.fechas_sac[idx] : "";
                t_l.Text = (this.db.libros[idx] != "---") ? this.db.libros[idx] : "";
                t_f.Text = (this.db.folios[idx] != "---") ? this.db.folios[idx] : "";
                t_a.Text = (this.db.asientos[idx] != "---") ? this.db.asientos[idx] : "";

                cargandoEdicion = false;
                actualizar_obs_auto_edit();
                f_edit.Invalidate();
            };

            Action<List<int>, bool> poblarTablaResultados = (indices, mostrarMensaje) =>
            {
                llenandoTabla = true;
                tablaResultados.Rows.Clear();
                for (int r = 0; r < indices.Count; r++)
                {
                    int idx = indices[r];
                    string dniMostrar = this.db.dnis_prin[idx];
                    string nomMostrar = this.db.nombres[idx];
                    string apeMostrar = this.db.apellidos[idx];

                    if (this.db.tipos[idx] == "Matrimonio" && this.db.dnis_sec[idx] != "---")
                    {
                        dniMostrar = this.db.dnis_prin[idx] + " / " + this.db.dnis_sec[idx];
                        nomMostrar = this.db.nombres[idx] + " / " + this.db.nombres_sec[idx];
                        apeMostrar = this.db.apellidos[idx] + " / " + this.db.apellidos_sec[idx];
                    }

                    int filaIdx = tablaResultados.Rows.Add(
                        dniMostrar,
                        nomMostrar,
                        apeMostrar,
                        this.db.tipos[idx],
                        this.db.estados[idx],
                        this.db.pagos[idx]
                    );
                    tablaResultados.Rows[filaIdx].Tag = idx;
                }
                llenandoTabla = false;

                if (indices.Count > 0)
                {
                    tablaResultados.Rows[0].Selected = true;
                    cargarExpedienteEnEditor(indices[0]);
                    if (mostrarMensaje)
                    {
                        MessageBox.Show("Se encontraron " + indices.Count + " expediente(s).", "Búsqueda Exitosa", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
                else
                {
                    f_edit.Visible = false;
                    if (mostrarMensaje)
                    {
                        MessageBox.Show("No se encontraron expedientes en trámite con ese criterio.", "Sin Resultados", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
            };

            tablaResultados.CellClick += (s, e) =>
            {
                if (!llenandoTabla && e.RowIndex >= 0 && tablaResultados.Rows[e.RowIndex].Tag != null)
                {
                    int idxSel = (int)tablaResultados.Rows[e.RowIndex].Tag;
                    cargarExpedienteEnEditor(idxSel);
                }
            };

            tablaResultados.SelectionChanged += (s, e) =>
            {
                if (!llenandoTabla && tablaResultados.SelectedRows.Count > 0 && tablaResultados.SelectedRows[0].Tag != null)
                {
                    int idxSel = (int)tablaResultados.SelectedRows[0].Tag;
                    if (idxSel != idx_actual)
                    {
                        cargarExpedienteEnEditor(idxSel);
                    }
                }
            };

            Action<bool> ejecutarBusqueda = (conAlerta) =>
            {
                string modo = cb_criterio.SelectedItem.ToString();
                if (modo == "DNI")
                {
                    string dni_val = txt_bus_dni.Text.Trim();
                    if (dni_val == "")
                    {
                        MessageBox.Show("No ha ingresado ningún número de DNI para buscar.", "Error de Búsqueda", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                    if (dni_val.Length != 8 || dni_val == "00000000")
                    {
                        MessageBox.Show("El DNI ingresado es incorrecto. Debe contener exactamente 8 dígitos numéricos válidos.", "Error de Búsqueda", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                    List<int> res = this.db.buscar_indices_por_dni(dni_val, true);
                    if (res.Count == 0)
                    {
                        List<int> resAntiguo = this.db.buscar_indices_por_dni(dni_val, false);
                        if (resAntiguo.Count > 0)
                        {
                            MessageBox.Show("El DNI " + dni_val + " pertenece a una Constancia Antigua registrada en 'Opciones Avanzadas'. Diríjase a esa pestaña para editarlo.", "Constancia Antigua", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            return;
                        }
                    }
                    poblarTablaResultados(res, conAlerta);
                }
                else
                {
                    string ape_val = Regex.Replace(BaseDatos.A_MayusculasConTildes(txt_bus_ape.Text), @"\s+", " ");
                    txt_bus_ape.Text = ape_val;
                    if (ape_val == "")
                    {
                        MessageBox.Show("No ha ingresado ningún apellido para buscar.", "Error de Búsqueda", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                    if (!this.EsNombreValido(ape_val))
                    {
                        MessageBox.Show("Ingrese al menos 2 letras válidas para buscar por apellidos.", "Error de Búsqueda", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                    List<int> res = this.db.buscar_indices_por_apellidos(ape_val, true);
                    if (res.Count == 0)
                    {
                        List<int> resAntiguo = this.db.buscar_indices_por_apellidos(ape_val, false);
                        if (resAntiguo.Count > 0)
                        {
                            MessageBox.Show("El apellido '" + ape_val + "' corresponde a una Constancia Antigua registrada en 'Opciones Avanzadas'.", "Constancia Antigua", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            return;
                        }
                    }
                    poblarTablaResultados(res, conAlerta);
                }
            };

            btnConsultar.Click += (s, e) => ejecutarBusqueda(true);

            txt_bus_dni.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    e.SuppressKeyPress = true;
                    ejecutarBusqueda(true);
                }
            };

            txt_bus_ape.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    e.SuppressKeyPress = true;
                    ejecutarBusqueda(true);
                }
            };

            btn_act.Click += (s, e) =>
            {
                if (idx_actual < 0 || idx_actual >= this.db.dnis_prin.Count)
                {
                    return;
                }

                int i = idx_actual;
                string tipo_actual = this.db.tipos[i];
                string dni1_val = t_dni1.Text.Trim();
                string nom1_val = Regex.Replace(BaseDatos.A_MayusculasConTildes(t_nom1.Text), @"\s+", " ");
                string ape1_val = Regex.Replace(BaseDatos.A_MayusculasConTildes(t_ape1.Text), @"\s+", " ");

                string dni2_val = "---";
                string nom2_val = "---";
                string ape2_val = "---";

                if (dni1_val.Length != 8 || dni1_val == "00000000")
                {
                    MessageBox.Show("El " + this.ObtenerEtiquetaDniPorSacramento(tipo_actual).Replace(":", "") + " debe tener 8 dígitos numéricos válidos.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                if (!this.EsNombreValido(nom1_val))
                {
                    MessageBox.Show("Los Nombres del titular deben contener al menos 2 letras válidas.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                if (!this.EsNombreValido(ape1_val))
                {
                    MessageBox.Show("Los Apellidos del titular deben contener al menos 2 letras válidas.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                if (tipo_actual == "Matrimonio")
                {
                    dni2_val = t_dni2.Text.Trim();
                    nom2_val = Regex.Replace(BaseDatos.A_MayusculasConTildes(t_nom2.Text), @"\s+", " ");
                    ape2_val = Regex.Replace(BaseDatos.A_MayusculasConTildes(t_ape2.Text), @"\s+", " ");

                    if (dni2_val.Length != 8 || dni2_val == "00000000")
                    {
                        MessageBox.Show("El DNI Novia debe tener 8 dígitos numéricos válidos.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                    if (!this.EsNombreValido(nom2_val) || !this.EsNombreValido(ape2_val))
                    {
                        MessageBox.Show("Los Nombres y Apellidos de la Novia son obligatorios.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                    if (dni1_val == dni2_val)
                    {
                        MessageBox.Show("No se pueden colocar 2 DNIs iguales. Los DNIs de los novios deben ser diferentes.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                    if ((nom1_val + " " + ape1_val) == (nom2_val + " " + ape2_val))
                    {
                        MessageBox.Show("Los nombres y apellidos de los novios no pueden ser iguales.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                }

                for (int idxExist = 0; idxExist < this.db.dnis_prin.Count; idxExist++)
                {
                    if (idxExist != i && this.db.tipos[idxExist] == tipo_actual)
                    {
                        if (this.db.dnis_prin[idxExist] == dni1_val || (tipo_actual == "Matrimonio" && this.db.dnis_sec[idxExist] == dni1_val))
                        {
                            MessageBox.Show("Ya existe otro expediente de " + tipo_actual + " con el DNI " + dni1_val + ".", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return;
                        }
                    }
                }

                if (tipo_actual == "Primera Comunión" && this.config.fecha_comunion == "--/--")
                {
                    MessageBox.Show("Debe configurar la fecha de inicio de Catequesis en 'Configuración de Fechas' antes de actualizar.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                if (tipo_actual == "Confirmación" && this.config.fecha_confirmacion == "--/--")
                {
                    MessageBox.Show("Debe configurar la fecha de inicio de Catequesis en 'Configuración de Fechas' antes de actualizar.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                string f_v = t_fec.Text.Trim();
                if (f_v == "")
                {
                    f_v = "Por Programar";
                }
                else
                {
                    DateTime fecha_obj;
                    if (!DateTime.TryParseExact(f_v, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out fecha_obj))
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
                    double deuda_float = 0;
                    if (deuda_nueva == "" || !double.TryParse(deuda_nueva, NumberStyles.Any, CultureInfo.InvariantCulture, out deuda_float) || deuda_float <= 0)
                    {
                        MessageBox.Show("Debe especificar un monto de deuda válido mayor a 0.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                }

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
                if (l_v == "") l_v = "---";

                string f_val = t_f.Text.Trim();
                if (f_val != "" && !this.EsNumeroPositivoValido(f_val))
                {
                    MessageBox.Show("El Folio solo admite números enteros positivos.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                if (f_val == "") f_val = "---";

                string a_v = t_a.Text.Trim();
                if (a_v != "" && !this.EsNumeroPositivoValido(a_v))
                {
                    MessageBox.Show("El Asiento solo admite números enteros positivos.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                if (a_v == "") a_v = "---";

                if (estado_elegido == "Inscrito(a)" && (!completos || pago_elegido == "Falta Pagar"))
                {
                    MessageBox.Show("No se puede establecer el estado como 'Inscrito(a)' si faltan requisitos físicos o tiene deuda pendiente.", "Estado Inconsistente", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
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
                        MessageBox.Show("Para finalizar el expediente debe completar los números positivos de Libro, Folio y Asiento.", "Datos de Registro Faltantes", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                }

                DialogResult res = MessageBox.Show("¿Está seguro de actualizar los datos en el sistema?", "Confirmar", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (res != DialogResult.Yes)
                {
                    return;
                }

                List<string> lista_chks_e = new List<string>();
                for (int j = 0; j < this.reqs_activos_edit; j++)
                {
                    lista_chks_e.Add(this.chks_edit[j].Checked ? "1" : "0");
                }
                string chks_str = string.Join("-", lista_chks_e);
                string t_pad_str = (tipo_actual == "Matrimonio") ? "SI" : (chk_e_padrinos.Checked ? "SI" : "NO");
                string est_pad_str = ((tipo_actual == "Bautizo" || tipo_actual == "Confirmación") && chk_e_padrinos.Checked) ? cb_e_est_pad.SelectedItem.ToString() : "---";

                this.db.actualizar_completo(
                    i,
                    dni1_val,
                    nom1_val,
                    ape1_val,
                    dni2_val,
                    nom2_val,
                    ape2_val,
                    "N/A",
                    tipo_actual,
                    estado_elegido,
                    t_pad_str,
                    est_pad_str,
                    chks_str,
                    txt_obs_auto_edit.Text.Trim(),
                    obs_adic_txt,
                    f_v,
                    l_v,
                    f_val,
                    a_v,
                    pago_elegido,
                    deuda_nueva
                );

                MessageBox.Show("Datos del expediente actualizados correctamente.", "Éxito", MessageBoxButtons.OK, MessageBoxIcon.Information);
                cargarExpedienteEnEditor(i);

                for (int r = 0; r < tablaResultados.Rows.Count; r++)
                {
                    if ((int)tablaResultados.Rows[r].Tag == i)
                    {
                        tablaResultados.Rows[r].Cells[0].Value = (tipo_actual == "Matrimonio" && dni2_val != "---") ? (dni1_val + " / " + dni2_val) : dni1_val;
                        tablaResultados.Rows[r].Cells[1].Value = (tipo_actual == "Matrimonio" && nom2_val != "---") ? (nom1_val + " / " + nom2_val) : nom1_val;
                        tablaResultados.Rows[r].Cells[2].Value = (tipo_actual == "Matrimonio" && ape2_val != "---") ? (ape1_val + " / " + ape2_val) : ape1_val;
                        tablaResultados.Rows[r].Cells[4].Value = estado_elegido;
                        tablaResultados.Rows[r].Cells[5].Value = pago_elegido;
                        break;
                    }
                }
            };

            btn_eli.Click += (s, e) =>
            {
                if (idx_actual < 0 || idx_actual >= this.db.dnis_prin.Count)
                {
                    return;
                }
                DialogResult res = MessageBox.Show("¿Está seguro de eliminar permanentemente el registro del sistema?", "Confirmar", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                if (res == DialogResult.Yes)
                {
                    this.db.eliminar(idx_actual);
                    idx_actual = -1;
                    f_edit.Visible = false;
                    tablaResultados.Rows.Clear();
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
            f_filtros.Size = new Size(940, 65);
            f_filtros.BackColor = ColorTranslator.FromHtml("#f1f5f9");
            f_filtros.BorderStyle = BorderStyle.FixedSingle;

            Label lblSac = new Label() { Text = "Sacramento:", Font = new Font("Segoe UI", 10f, FontStyle.Bold), Location = new Point(15, 22), AutoSize = true };
            ComboBox cb_filtro_sac = new ComboBox() { DropDownStyle = ComboBoxStyle.DropDownList, Location = new Point(110, 19), Width = 150 };
            cb_filtro_sac.Items.AddRange(new string[] { "Todos", "Bautizo", "Primera Comunión", "Confirmación", "Matrimonio" });
            cb_filtro_sac.SelectedItem = "Todos";

            Label lblFecOf = new Label() { Text = "Fecha Oficial:\n(DD/MM/AAAA)", Font = new Font("Segoe UI", 9.5f, FontStyle.Bold), Location = new Point(280, 12), AutoSize = true };
            TextBox txt_filtro_fec = new TextBox() { Location = new Point(400, 19), Width = 110 };
            this.adjuntar_validacion(txt_filtro_fec, "fecha");

            Label lblOrd = new Label() { Text = "Ordenar:", Font = new Font("Segoe UI", 10f, FontStyle.Bold), Location = new Point(530, 22), AutoSize = true };
            ComboBox cb_orden = new ComboBox() { DropDownStyle = ComboBoxStyle.DropDownList, Location = new Point(600, 19), Width = 150 };
            cb_orden.Items.AddRange(new string[] { "Ascendente (A-Z)", "Descendente (Z-A)" });
            cb_orden.SelectedItem = "Ascendente (A-Z)";

            Button btnFiltrar = CrearBotonAccion("Aplicar Filtros", ColorTranslator.FromHtml("#2563eb"), new Point(770, 15), new Size(140, 34));

            f_filtros.Controls.AddRange(new Control[] { lblSac, cb_filtro_sac, lblFecOf, txt_filtro_fec, lblOrd, cb_orden, btnFiltrar });
            this.content_frame.Controls.Add(f_filtros);

            DataGridView tabla = new DataGridView();
            tabla.Location = new Point(40, 140);
            tabla.Size = new Size(940, 420);
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

                for (int i = 0; i < this.db.dnis_prin.Count; i++)
                {
                    if (f_sac != "Todos" && this.db.tipos[i] != f_sac)
                    {
                        continue;
                    }
                    if (f_fec != "" && this.db.fechas_sac[i] != f_fec)
                    {
                        continue;
                    }

                    string nombre_display = this.db.ObtenerNombreCompletoTitular(i);
                    if (this.db.tipos[i] == "Matrimonio" && this.db.ObtenerNombreCompletoConyuge(i) != "")
                    {
                        nombre_display = nombre_display + ",\n" + this.db.ObtenerNombreCompletoConyuge(i);
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

            Button btnExportar = CrearBotonAccion("Exportar Resumen PDF", ColorTranslator.FromHtml("#10b981"), new Point(385, 580), new Size(250, 44), 12f);

            btnExportar.Click += (s, e) =>
            {
                if (this.db.dnis_prin.Count == 0)
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

                for (int i = 0; i < this.db.dnis_prin.Count; i++)
                {
                    if (f_sac != "Todos" && this.db.tipos[i] != f_sac)
                    {
                        continue;
                    }
                    if (f_fec != "" && this.db.fechas_sac[i] != f_fec)
                    {
                        continue;
                    }

                    string nom = this.db.ObtenerNombreCompletoTitular(i);
                    if (this.db.tipos[i] == "Matrimonio" && this.db.ObtenerNombreCompletoConyuge(i) != "")
                    {
                        nom = nom + ", " + this.db.ObtenerNombreCompletoConyuge(i);
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

        // ================= 8. OPCIONES AVANZADAS: CONSTANCIAS ANTIGUAS =================
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
            lblSub.Text = "Registro, edición y eliminación de constancias inscritas anteriormente (Con DNI o Sin DNI)";
            lblSub.Font = new Font("Segoe UI", 10.5f, FontStyle.Regular);
            lblSub.ForeColor = ColorTranslator.FromHtml("#64748b");
            lblSub.Location = new Point(40, 50);
            lblSub.AutoSize = true;
            this.content_frame.Controls.Add(lblSub);

            Panel f_form = new Panel();
            f_form.Location = new Point(40, 80);
            f_form.Size = new Size(940, 275);
            f_form.BackColor = Color.White;
            f_form.BorderStyle = BorderStyle.FixedSingle;

            int idx_hist_seleccionado = -1;
            bool cargandoDesdeTabla = false;
            bool llenandoTablaHist = false;

            Label lblSac = new Label() { Text = "Sacramento:", Font = new Font("Segoe UI", 10f, FontStyle.Bold), Location = new Point(25, 20), AutoSize = true };
            ComboBox cb_sac = new ComboBox() { DropDownStyle = ComboBoxStyle.DropDownList, Location = new Point(160, 17), Width = 190 };
            cb_sac.Items.AddRange(new string[] { "Bautizo", "Primera Comunión", "Confirmación", "Matrimonio" });

            Label lblModoDni = new Label() { Text = "Tipo Registro:", Font = new Font("Segoe UI", 10f, FontStyle.Bold), Location = new Point(385, 20), AutoSize = true };
            ComboBox cb_modo_dni = new ComboBox() { DropDownStyle = ComboBoxStyle.DropDownList, Location = new Point(495, 17), Width = 150 };
            cb_modo_dni.Items.AddRange(new string[] { "Con DNI", "Sin DNI" });

            f_form.Controls.AddRange(new Control[] { lblSac, cb_sac, lblModoDni, cb_modo_dni });

            Label lblDni1 = new Label() { Text = "DNI Bautizado:", Font = new Font("Segoe UI", 10f, FontStyle.Bold), Location = new Point(25, 65), Size = new Size(132, 25) };
            TextBox txt_dni1 = new TextBox() { Location = new Point(160, 62), Width = 110 };
            this.adjuntar_validacion(txt_dni1, "dni");

            Label lblNom1 = new Label() { Text = "Nombre:", Font = new Font("Segoe UI", 10f, FontStyle.Bold), Location = new Point(285, 65), Size = new Size(70, 25) };
            TextBox txt_nom1 = new TextBox() { Location = new Point(360, 62), Width = 215 };
            AsignarPlaceholder(txt_nom1, "NOMBRES");
            this.adjuntar_validacion(txt_nom1, "texto");

            Label lblApe1 = new Label() { Text = "Apellido:", Font = new Font("Segoe UI", 10f, FontStyle.Bold), Location = new Point(590, 65), Size = new Size(70, 25) };
            TextBox txt_ape1 = new TextBox() { Location = new Point(665, 62), Width = 240 };
            AsignarPlaceholder(txt_ape1, "APELLIDOS");
            this.adjuntar_validacion(txt_ape1, "texto");

            f_form.Controls.AddRange(new Control[] { lblDni1, txt_dni1, lblNom1, txt_nom1, lblApe1, txt_ape1 });

            Label lblDni2 = new Label() { Text = "DNI Novia:", Font = new Font("Segoe UI", 10f, FontStyle.Bold), Location = new Point(25, 108), Size = new Size(132, 25), Visible = false };
            TextBox txt_dni2 = new TextBox() { Location = new Point(160, 105), Width = 110, Visible = false };
            this.adjuntar_validacion(txt_dni2, "dni");

            Label lblNom2 = new Label() { Text = "Nombre:", Font = new Font("Segoe UI", 10f, FontStyle.Bold), Location = new Point(285, 108), Size = new Size(70, 25), Visible = false };
            TextBox txt_nom2 = new TextBox() { Location = new Point(360, 105), Width = 215, Visible = false };
            AsignarPlaceholder(txt_nom2, "NOMBRES NOVIA");
            this.adjuntar_validacion(txt_nom2, "texto");

            Label lblApe2 = new Label() { Text = "Apellido:", Font = new Font("Segoe UI", 10f, FontStyle.Bold), Location = new Point(590, 108), Size = new Size(70, 25), Visible = false };
            TextBox txt_ape2 = new TextBox() { Location = new Point(665, 105), Width = 240, Visible = false };
            AsignarPlaceholder(txt_ape2, "APELLIDOS NOVIA");
            this.adjuntar_validacion(txt_ape2, "texto");

            f_form.Controls.AddRange(new Control[] { lblDni2, txt_dni2, lblNom2, txt_nom2, lblApe2, txt_ape2 });

            this.VincularAutocompletadoReniecSeparado(txt_dni1, txt_nom1, txt_ape1, txt_dni2, () => cargandoDesdeTabla);
            this.VincularAutocompletadoReniecSeparado(txt_dni2, txt_nom2, txt_ape2, txt_dni1, () => cargandoDesdeTabla);

            Label lblFec = new Label() { Text = "Fecha:", Font = new Font("Segoe UI", 10f, FontStyle.Bold), Location = new Point(25, 155), Size = new Size(95, 25) };
            TextBox txt_fec = new TextBox() { Location = new Point(160, 152), Width = 130 };
            AsignarPlaceholder(txt_fec, "DD/MM/AAAA");
            this.adjuntar_validacion(txt_fec, "fecha");

            Label lblLib = new Label() { Text = "Libro:", Font = new Font("Segoe UI", 10f, FontStyle.Bold), Location = new Point(315, 155), AutoSize = true };
            TextBox txt_lib = new TextBox() { Location = new Point(370, 152), Width = 90 };
            AsignarPlaceholder(txt_lib, "---");
            this.adjuntar_validacion(txt_lib, "registro_libro");

            Label lblFol = new Label() { Text = "Folio:", Font = new Font("Segoe UI", 10f, FontStyle.Bold), Location = new Point(490, 155), AutoSize = true };
            TextBox txt_fol = new TextBox() { Location = new Point(545, 152), Width = 90 };
            AsignarPlaceholder(txt_fol, "---");
            this.adjuntar_validacion(txt_fol, "registro_libro");

            Label lblAsi = new Label() { Text = "Asiento:", Font = new Font("Segoe UI", 10f, FontStyle.Bold), Location = new Point(665, 155), AutoSize = true };
            TextBox txt_asi = new TextBox() { Location = new Point(735, 152), Width = 90 };
            AsignarPlaceholder(txt_asi, "---");
            this.adjuntar_validacion(txt_asi, "registro_libro");

            f_form.Controls.AddRange(new Control[] { lblFec, txt_fec, lblLib, txt_lib, lblFol, txt_fol, lblAsi, txt_asi });

            Action actualizarVistaDniSac = () =>
            {
                string sacSel = cb_sac.SelectedItem != null ? cb_sac.SelectedItem.ToString() : "Bautizo";
                bool esMatrimonio = (sacSel == "Matrimonio");
                bool conDni = (cb_modo_dni.SelectedItem == null || cb_modo_dni.SelectedItem.ToString() == "Con DNI");

                lblDni1.Text = this.ObtenerEtiquetaDniPorSacramento(sacSel);
                lblDni1.Visible = conDni;
                txt_dni1.Visible = conDni;
                txt_dni1.Enabled = conDni;

                lblDni2.Visible = (esMatrimonio && conDni);
                txt_dni2.Visible = (esMatrimonio && conDni);
                txt_dni2.Enabled = (esMatrimonio && conDni);

                lblNom2.Visible = esMatrimonio;
                txt_nom2.Visible = esMatrimonio;
                lblApe2.Visible = esMatrimonio;
                txt_ape2.Visible = esMatrimonio;

                if (!conDni)
                {
                    txt_dni1.Text = "";
                    txt_dni2.Text = "";
                }
                if (!esMatrimonio)
                {
                    txt_dni2.Text = "";
                    txt_nom2.Text = "";
                    txt_ape2.Text = "";
                }
                f_form.Invalidate();
            };

            cb_sac.SelectedIndexChanged += (s, e) => actualizarVistaDniSac();
            cb_modo_dni.SelectedIndexChanged += (s, e) => actualizarVistaDniSac();

            cb_sac.SelectedItem = "Bautizo";
            cb_modo_dni.SelectedItem = "Con DNI";

            Button btnRegistrar = CrearBotonAccion("Registrar", ColorTranslator.FromHtml("#2563eb"), new Point(25, 210), new Size(205, 42), 10.5f);
            Button btnEditar = CrearBotonAccion("Editar / Actualizar", ColorTranslator.FromHtml("#10b981"), new Point(250, 210), new Size(205, 42), 10.5f);
            Button btnEliminar = CrearBotonAccion("Eliminar", ColorTranslator.FromHtml("#ef4444"), new Point(475, 210), new Size(205, 42), 10.5f);
            Button btnLimpiar = CrearBotonAccion("Limpiar Campos", ColorTranslator.FromHtml("#64748b"), new Point(700, 210), new Size(205, 42), 10.5f);

            f_form.Controls.AddRange(new Control[] { btnRegistrar, btnEditar, btnEliminar, btnLimpiar });
            this.content_frame.Controls.Add(f_form);

            DataGridView tablaHist = new DataGridView();
            tablaHist.Location = new Point(40, 370);
            tablaHist.Size = new Size(940, 320);
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
                llenandoTablaHist = true;
                tablaHist.Rows.Clear();
                for (int i = 0; i < this.db.dnis_prin.Count; i++)
                {
                    if (this.db.origenes[i] != "ANTIGUO")
                    {
                        continue;
                    }

                    string dniMostrar = this.db.dnis_prin[i];
                    string nomMostrar = this.db.nombres[i];
                    string apeMostrar = this.db.apellidos[i];

                    if (this.db.tipos[i] == "Matrimonio" && this.db.nombres_sec[i] != "---" && this.db.nombres_sec[i] != "")
                    {
                        dniMostrar = this.db.dnis_prin[i] + "\n" + this.db.dnis_sec[i];
                        nomMostrar = this.db.nombres[i] + "\n" + this.db.nombres_sec[i];
                        apeMostrar = this.db.apellidos[i] + "\n" + this.db.apellidos_sec[i];
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
                tablaHist.ClearSelection();
                llenandoTablaHist = false;
            };

            Action limpiarCamposHist = () =>
            {
                cargandoDesdeTabla = true;
                idx_hist_seleccionado = -1;
                cb_sac.SelectedItem = "Bautizo";
                cb_modo_dni.SelectedItem = "Con DNI";
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
                actualizarVistaDniSac();
            };

            Action<int> cargarRegistroEnFormulario = (idx) =>
            {
                if (idx < 0 || idx >= this.db.dnis_prin.Count)
                {
                    return;
                }
                cargandoDesdeTabla = true;
                idx_hist_seleccionado = idx;

                cb_sac.SelectedItem = this.db.tipos[idx];

                bool registroSinDni = (this.db.dnis_prin[idx] == "SIN DNI" || this.db.dnis_prin[idx] == "---" || this.db.dnis_prin[idx] == "");
                cb_modo_dni.SelectedItem = registroSinDni ? "Sin DNI" : "Con DNI";
                actualizarVistaDniSac();

                txt_dni1.Text = registroSinDni ? "" : this.db.dnis_prin[idx];
                txt_nom1.Text = BaseDatos.A_MayusculasConTildes(this.db.nombres[idx]);
                txt_ape1.Text = BaseDatos.A_MayusculasConTildes(this.db.apellidos[idx]);

                if (this.db.tipos[idx] == "Matrimonio")
                {
                    txt_dni2.Text = (registroSinDni || this.db.dnis_sec[idx] == "---" || this.db.dnis_sec[idx] == "SIN DNI") ? "" : this.db.dnis_sec[idx];
                    txt_nom2.Text = (this.db.nombres_sec[idx] != "---") ? BaseDatos.A_MayusculasConTildes(this.db.nombres_sec[idx]) : "";
                    txt_ape2.Text = (this.db.apellidos_sec[idx] != "---") ? BaseDatos.A_MayusculasConTildes(this.db.apellidos_sec[idx]) : "";
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
                if (!llenandoTablaHist && e.RowIndex >= 0 && tablaHist.Rows[e.RowIndex].Tag != null)
                {
                    int idxReal = (int)tablaHist.Rows[e.RowIndex].Tag;
                    cargarRegistroEnFormulario(idxReal);
                }
            };

            tablaHist.SelectionChanged += (s, e) =>
            {
                if (!llenandoTablaHist && tablaHist.SelectedRows.Count > 0 && tablaHist.SelectedRows[0].Tag != null)
                {
                    int idxReal = (int)tablaHist.SelectedRows[0].Tag;
                    if (idxReal != idx_hist_seleccionado)
                    {
                        cargarRegistroEnFormulario(idxReal);
                    }
                }
            };

            btnLimpiar.Click += (s, e) => limpiarCamposHist();

            Func<int, bool> validarFormularioHistorico = (idxIgnorar) =>
            {
                string sac = cb_sac.SelectedItem.ToString();
                bool conDni = (cb_modo_dni.SelectedItem.ToString() == "Con DNI");

                string d1 = conDni ? txt_dni1.Text.Trim() : "SIN DNI";
                string n1 = Regex.Replace(BaseDatos.A_MayusculasConTildes(txt_nom1.Text), @"\s+", " ");
                string a1 = Regex.Replace(BaseDatos.A_MayusculasConTildes(txt_ape1.Text), @"\s+", " ");

                string d2 = (sac == "Matrimonio") ? (conDni ? txt_dni2.Text.Trim() : "SIN DNI") : "---";
                string n2 = Regex.Replace(BaseDatos.A_MayusculasConTildes(txt_nom2.Text), @"\s+", " ");
                string a2 = Regex.Replace(BaseDatos.A_MayusculasConTildes(txt_ape2.Text), @"\s+", " ");

                string fec = txt_fec.Text.Trim();
                string lib = txt_lib.Text.Trim();
                string fol = txt_fol.Text.Trim();
                string asi = txt_asi.Text.Trim();

                List<string> errores = new List<string>();

                if (conDni)
                {
                    if (d1.Length != 8 || d1 == "00000000")
                    {
                        errores.Add("- El " + this.ObtenerEtiquetaDniPorSacramento(sac).Replace(":", "") + " debe tener exactamente 8 dígitos numéricos válidos.");
                    }
                }
                if (!this.EsNombreValido(n1))
                {
                    errores.Add("- El Nombre es obligatorio (mínimo 2 letras).");
                }
                if (!this.EsNombreValido(a1))
                {
                    errores.Add("- El Apellido es obligatorio (mínimo 2 letras).");
                }

                if (sac == "Matrimonio")
                {
                    if (conDni)
                    {
                        if (d2.Length != 8 || d2 == "00000000")
                        {
                            errores.Add("- El DNI Novia debe tener 8 dígitos numéricos válidos.");
                        }
                        if (d1 != "" && d1 == d2)
                        {
                            errores.Add("- No puede ingresar 2 DNIs iguales. Los DNIs de los esposos deben ser diferentes.");
                        }
                    }
                    if (!this.EsNombreValido(n2))
                    {
                        errores.Add("- El Nombre de la Novia es obligatorio.");
                    }
                    if (!this.EsNombreValido(a2))
                    {
                        errores.Add("- El Apellido de la Novia es obligatorio.");
                    }
                    if (n1 != "" && a1 != "" && (n1 + " " + a1) == (n2 + " " + a2))
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
                        if (conDni)
                        {
                            if (this.db.dnis_prin[i] == d1 || (sac == "Matrimonio" && this.db.dnis_sec[i] == d1))
                            {
                                errores.Add("- El DNI " + d1 + " ya se encuentra registrado en " + sac + ".");
                                break;
                            }
                            if (sac == "Matrimonio" && (this.db.dnis_prin[i] == d2 || this.db.dnis_sec[i] == d2))
                            {
                                errores.Add("- El DNI " + d2 + " ya se encuentra registrado en " + sac + ".");
                                break;
                            }
                        }
                        else
                        {
                            if (this.db.nombres[i] == n1 && this.db.apellidos[i] == a1)
                            {
                                errores.Add("- Ya existe un registro de " + sac + " para " + n1 + " " + a1 + ".");
                                break;
                            }
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
                bool conDni = (cb_modo_dni.SelectedItem.ToString() == "Con DNI");

                string d1 = conDni ? txt_dni1.Text.Trim() : "SIN DNI";
                string n1 = Regex.Replace(BaseDatos.A_MayusculasConTildes(txt_nom1.Text), @"\s+", " ");
                string a1 = Regex.Replace(BaseDatos.A_MayusculasConTildes(txt_ape1.Text), @"\s+", " ");

                string d2 = "---";
                string n2 = "---";
                string a2 = "---";

                if (sac == "Matrimonio")
                {
                    d2 = conDni ? txt_dni2.Text.Trim() : "SIN DNI";
                    n2 = Regex.Replace(BaseDatos.A_MayusculasConTildes(txt_nom2.Text), @"\s+", " ");
                    a2 = Regex.Replace(BaseDatos.A_MayusculasConTildes(txt_ape2.Text), @"\s+", " ");
                }

                this.db.registrar(
                    "ANTIGUO",
                    d1,
                    n1,
                    a1,
                    d2,
                    n2,
                    a2,
                    "N/A",
                    sac,
                    "Finalizado",
                    "NO",
                    "---",
                    "1-1-1",
                    "Constancia histórica registrada.",
                    "",
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
                bool conDni = (cb_modo_dni.SelectedItem.ToString() == "Con DNI");

                string d1 = conDni ? txt_dni1.Text.Trim() : "SIN DNI";
                string n1 = Regex.Replace(BaseDatos.A_MayusculasConTildes(txt_nom1.Text), @"\s+", " ");
                string a1 = Regex.Replace(BaseDatos.A_MayusculasConTildes(txt_ape1.Text), @"\s+", " ");

                string d2 = "---";
                string n2 = "---";
                string a2 = "---";

                if (sac == "Matrimonio")
                {
                    d2 = conDni ? txt_dni2.Text.Trim() : "SIN DNI";
                    n2 = Regex.Replace(BaseDatos.A_MayusculasConTildes(txt_nom2.Text), @"\s+", " ");
                    a2 = Regex.Replace(BaseDatos.A_MayusculasConTildes(txt_ape2.Text), @"\s+", " ");
                }

                this.db.actualizar_completo(
                    idx_hist_seleccionado,
                    d1,
                    n1,
                    a1,
                    d2,
                    n2,
                    a2,
                    "N/A",
                    sac,
                    "Finalizado",
                    "NO",
                    "---",
                    "1-1-1",
                    "Constancia histórica registrada.",
                    "",
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