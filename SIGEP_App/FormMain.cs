using System;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace SIGEP_App
{
    public partial class FormMain : Form
    {
        private ToolTip guiaCampos = new ToolTip();

        private void AsignarPlaceholder(TextBox caja, string texto)
        {
            this.guiaCampos.SetToolTip(caja, texto);
        }

        private Font FuentePx(float tamanoPx, FontStyle estilo = FontStyle.Regular)
        {
            return new Font("Segoe UI", tamanoPx, estilo, GraphicsUnit.Pixel);
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

        // Arreglos puros de controles CheckBox
        private CheckBox[] chks = new CheckBox[9];
        private int reqs_activos = 0;

        private CheckBox[] chks_edit = new CheckBox[9];
        private int reqs_activos_edit = 0;

        // Arreglos paralelos dinámicos para pintar bordes de validación
        private int capBordes = 60;
        private Control[] controlesBorde = new Control[60];
        private Color[] coloresBorde = new Color[60];
        private int totalControlesBorde = 0;

        private Control[] panelesConPaint = new Control[30];
        private int totalPanelesPaint = 0;

        public FormMain()
        {
            this.AutoScaleMode = AutoScaleMode.None;
            this.Text = "SIGEP Workspace - Sistema de Gestión Parroquial";
            this.MinimumSize = new Size(960, 640);
            this.WindowState = FormWindowState.Maximized;
            this.BackColor = ColorTranslator.FromHtml("#f8fafc");
            this.Font = FuentePx(13.5f, FontStyle.Regular);

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

        private Button CrearBotonAccion(string texto, Color colorFondo, Point ubicacion, Size tamano, float tamFuentePx = 14f)
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
            btn.Font = FuentePx(tamFuentePx, FontStyle.Bold);
            btn.TextAlign = ContentAlignment.MiddleCenter;
            btn.Cursor = Cursors.Hand;
            return btn;
        }

        private void ConfigurarTablaResponsiva(DataGridView tabla)
        {
            tabla.BackgroundColor = Color.White;
            tabla.BorderStyle = BorderStyle.FixedSingle;
            tabla.AllowUserToAddRows = false;
            tabla.AllowUserToResizeRows = false;
            tabla.ReadOnly = true;
            tabla.RowHeadersVisible = false;
            tabla.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            tabla.MultiSelect = false;
            tabla.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            tabla.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            tabla.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;
            tabla.DefaultCellStyle.WrapMode = DataGridViewTriState.True;
            tabla.DefaultCellStyle.Font = FuentePx(13f, FontStyle.Regular);
            tabla.DefaultCellStyle.Padding = new Padding(5, 5, 5, 5);
            tabla.ColumnHeadersDefaultCellStyle.Font = FuentePx(13.5f, FontStyle.Bold);
            tabla.ColumnHeadersDefaultCellStyle.Padding = new Padding(5, 6, 5, 6);
            tabla.ColumnHeadersDefaultCellStyle.WrapMode = DataGridViewTriState.True;
            tabla.RowTemplate.MinimumHeight = 32;
        }

        private void DistribuirFilaPersona(Panel panelContenedor, int topY, bool mostrarDni, Label lblDni, TextBox txtDni, Label lblNom, TextBox txtNom, Label lblApe, TextBox txtApe)
        {
            int margenIzq = 25;
            int margenDer = 25;
            int xActual = margenIzq;

            if (mostrarDni && lblDni != null && txtDni != null)
            {
                lblDni.Location = new Point(xActual, topY + 4);
                xActual = lblDni.Right + 8;

                txtDni.Location = new Point(xActual, topY);
                txtDni.Size = new Size(105, 27);
                xActual = txtDni.Right + 18;
            }

            lblNom.Location = new Point(xActual, topY + 4);
            xActual = lblNom.Right + 8;

            int anchoRestante = Math.Max(260, panelContenedor.ClientSize.Width - xActual - margenDer);
            int anchoEtiquetaApe = lblApe.PreferredWidth + 24;
            int anchoCajas = Math.Max(180, anchoRestante - anchoEtiquetaApe);
            int anchoNom = (int)(anchoCajas * 0.48);
            int anchoApe = anchoCajas - anchoNom;

            txtNom.Location = new Point(xActual, topY);
            txtNom.Size = new Size(anchoNom, 27);
            xActual = txtNom.Right + 16;

            lblApe.Location = new Point(xActual, topY + 4);
            xActual = lblApe.Right + 8;

            txtApe.Location = new Point(xActual, topY);
            txtApe.Size = new Size(Math.Max(90, anchoApe), 27);
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

        private string UnirArregloConSeparador(string[] arreglo, int cantidad, string separador)
        {
            if (cantidad <= 0)
            {
                return "";
            }
            string resultado = arreglo[0];
            for (int i = 1; i < cantidad; i++)
            {
                resultado = resultado + separador + arreglo[i];
            }
            return resultado;
        }

        // ================= MENÚ DESPLEGABLE SUPERIOR =================
        private void ConstruirMenuSuperior()
        {
            this.menuPrincipal = new MenuStrip();
            this.menuPrincipal.BackColor = Color.White;
            this.menuPrincipal.ForeColor = ColorTranslator.FromHtml("#0f172a");
            this.menuPrincipal.Font = FuentePx(13f, FontStyle.Regular);
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
            ventanaAcerca.AutoScaleMode = AutoScaleMode.None;
            ventanaAcerca.Text = "Acerca de SIGEP";
            ventanaAcerca.Size = new Size(535, 400);
            ventanaAcerca.FormBorderStyle = FormBorderStyle.FixedDialog;
            ventanaAcerca.StartPosition = FormStartPosition.CenterParent;
            ventanaAcerca.MaximizeBox = false;
            ventanaAcerca.MinimizeBox = false;
            ventanaAcerca.BackColor = ColorTranslator.FromHtml("#f0f0f0");
            ventanaAcerca.Font = FuentePx(12.5f, FontStyle.Regular);

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

                using (Font fBanner = FuentePx(28f, FontStyle.Bold | FontStyle.Italic))
                using (SolidBrush textoBrush = new SolidBrush(Color.White))
                {
                    e.Graphics.DrawString("SIGEP", fBanner, textoBrush, new PointF(14, 15));
                }

                using (Font fSub = FuentePx(11.5f, FontStyle.Regular))
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

            Label lblVersion = new Label() { Text = "SIGEP 1.0 (64-bit)", Location = new Point(98, 118), AutoSize = true };
            Label lblCopy = new Label() { Text = "Copyright © 2026 por\nEquipo de Desarrollo SIGEP", Location = new Point(98, 142), AutoSize = true };
            Label lblPublicado = new Label() { Text = "Publicado para Parroquia San José", Location = new Point(98, 188), AutoSize = true };
            Label lblSede = new Label() { Text = "Sede Institucional:\nBellavista, Diócesis del Callao", Location = new Point(98, 220), AutoSize = true };

            Panel linea1 = new Panel() { Location = new Point(98, 272), Size = new Size(230, 1), BackColor = ColorTranslator.FromHtml("#b4b4b4") };
            Label lblLicenciaEstado = new Label() { Text = "Licencia de uso institucional activa", Location = new Point(98, 282), AutoSize = true };
            Panel linea2 = new Panel() { Location = new Point(98, 326), Size = new Size(230, 1), BackColor = ColorTranslator.FromHtml("#b4b4b4") };

            ventanaAcerca.Controls.AddRange(new Control[] { lblVersion, lblCopy, lblPublicado, lblSede, linea1, lblLicenciaEstado, linea2 });

            Button btnAceptar = new Button() { Text = "Aceptar", Location = new Point(352, 16), Size = new Size(145, 30), BackColor = Color.White, ForeColor = Color.Black, FlatStyle = FlatStyle.Standard };
            btnAceptar.Click += (s, e) => ventanaAcerca.Close();

            Button btnLicencia = new Button() { Text = "Licencia", Location = new Point(352, 54), Size = new Size(145, 30), BackColor = Color.White, ForeColor = Color.Black, FlatStyle = FlatStyle.Standard };
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

            Button btnContacto = new Button() { Text = "Contacto", Location = new Point(352, 92), Size = new Size(145, 30), BackColor = Color.White, ForeColor = Color.Black, FlatStyle = FlatStyle.Standard };
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

            ventanaAcerca.Controls.AddRange(new Control[] { btnAceptar, btnLicencia, btnContacto });
            ventanaAcerca.AcceptButton = btnAceptar;
            ventanaAcerca.ShowDialog(this);
        }

        private void limpiar_contenedor()
        {
            this.container.Controls.Clear();
            this.totalControlesBorde = 0;
            this.totalPanelesPaint = 0;
        }

        private void limpiar_contenido()
        {
            this.content_frame.Controls.Clear();
            this.totalControlesBorde = 0;
            this.totalPanelesPaint = 0;
        }

        // ================= MOTOR DE BORDES CON ARREGLOS PARALELOS DINÁMICOS =================
        private void AsegurarCapacidadBordes()
        {
            if (this.totalControlesBorde >= this.capBordes)
            {
                int nuevaCap = this.capBordes * 2;
                Control[] nuevoCtrl = new Control[nuevaCap];
                Color[] nuevoCol = new Color[nuevaCap];
                for (int i = 0; i < this.totalControlesBorde; i++)
                {
                    nuevoCtrl[i] = this.controlesBorde[i];
                    nuevoCol[i] = this.coloresBorde[i];
                }
                this.controlesBorde = nuevoCtrl;
                this.coloresBorde = nuevoCol;
                this.capBordes = nuevaCap;
            }
        }

        private int BuscarIndiceControlBorde(Control ctrl)
        {
            for (int i = 0; i < this.totalControlesBorde; i++)
            {
                if (this.controlesBorde[i] == ctrl)
                {
                    return i;
                }
            }
            return -1;
        }

        private bool ExistePanelConPaint(Control pnl)
        {
            for (int i = 0; i < this.totalPanelesPaint; i++)
            {
                if (this.panelesConPaint[i] == pnl)
                {
                    return true;
                }
            }
            return false;
        }

        private void ActualizarBorde(Control ctrl, Color color)
        {
            int pos = this.BuscarIndiceControlBorde(ctrl);
            if (pos != -1)
            {
                this.coloresBorde[pos] = color;
            }
            else
            {
                this.AsegurarCapacidadBordes();
                this.controlesBorde[this.totalControlesBorde] = ctrl;
                this.coloresBorde[this.totalControlesBorde] = color;
                this.totalControlesBorde++;
            }

            if (ctrl.Parent != null)
            {
                if (!this.ExistePanelConPaint(ctrl.Parent) && this.totalPanelesPaint < this.panelesConPaint.Length)
                {
                    this.panelesConPaint[this.totalPanelesPaint] = ctrl.Parent;
                    this.totalPanelesPaint++;
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
                if (c.Visible)
                {
                    int idx = this.BuscarIndiceControlBorde(c);
                    if (idx != -1)
                    {
                        Color col = this.coloresBorde[idx];
                        using (Pen pen = new Pen(col, 2f))
                        {
                            e.Graphics.DrawRectangle(pen, c.Left - 1, c.Top - 1, c.Width + 1, c.Height + 1);
                        }
                    }
                }
            }
        }

        private bool ignorarEvento = false;

        private void adjuntar_validacion(TextBox entry_widget, string tipo)
        {
            entry_widget.BorderStyle = BorderStyle.FixedSingle;
            entry_widget.Font = FuentePx(13.5f, FontStyle.Regular);

            entry_widget.VisibleChanged += (s, e) =>
            {
                if (entry_widget.Parent != null)
                {
                    entry_widget.Parent.Invalidate();
                }
            };

            entry_widget.SizeChanged += (s, e) =>
            {
                if (entry_widget.Parent != null)
                {
                    entry_widget.Parent.Invalidate();
                }
            };

            entry_widget.LocationChanged += (s, e) =>
            {
                if (entry_widget.Parent != null)
                {
                    entry_widget.Parent.Invalidate();
                }
            };

            entry_widget.HandleCreated += (s, e) =>
            {
                if (this.BuscarIndiceControlBorde(entry_widget) == -1)
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
            return soloLetras.Length >= 2;
        }

        private bool EsNumeroPositivoValido(string texto)
        {
            if (string.IsNullOrWhiteSpace(texto))
            {
                return false;
            }
            int num = 0;
            return int.TryParse(texto.Trim(), out num) && num > 0;
        }

        private string[] ObtenerRequisitosSacramento(string sacramento, bool tienePadrinos, string estadoCivilPadrinos)
        {
            string[] temp = new string[9];
            int cant = 0;

            if (sacramento == "Bautizo")
            {
                temp[cant++] = "1. Copia DNI Bautizado";
                temp[cant++] = "2. Partida Nacimiento";
                temp[cant++] = "3. Copia DNI Padre";
                temp[cant++] = "4. Copia DNI Madre";
                if (tienePadrinos)
                {
                    temp[cant++] = "5. Copia DNI Padrino / Madrina";
                    temp[cant++] = (estadoCivilPadrinos == "Casado(a)")
                        ? "6. Partida Matrimonio Religioso Padrinos"
                        : "6. Constancia Confirmación Padrino/Madrina";
                }
            }
            else if (sacramento == "Primera Comunión")
            {
                temp[cant++] = "1. Copia DNI Comulgante";
                temp[cant++] = "2. Partida Bautismo";
                temp[cant++] = "3. Copia DNI Padre";
                temp[cant++] = "4. Copia DNI Madre";
                if (tienePadrinos)
                {
                    temp[cant++] = "5. Copia DNI Padrino / Madrina";
                }
            }
            else if (sacramento == "Confirmación")
            {
                temp[cant++] = "1. Copia DNI Confirmando";
                temp[cant++] = "2. Partida Bautismo";
                temp[cant++] = "3. Constancia 1ra Comunión";
                if (tienePadrinos)
                {
                    temp[cant++] = "4. Copia DNI Padrino / Madrina";
                    temp[cant++] = (estadoCivilPadrinos == "Casado(a)")
                        ? "5. Partida Matrimonio Religioso Padrinos"
                        : "5. Constancia Confirmación Padrino/Madrina";
                }
            }
            else if (sacramento == "Matrimonio")
            {
                temp[cant++] = "1. Copia DNI Novio";
                temp[cant++] = "2. Copia DNI Novia";
                temp[cant++] = "3. Partida Bautismo Novio";
                temp[cant++] = "4. Partida Bautismo Novia";
                temp[cant++] = "5. Confirmación Novio";
                temp[cant++] = "6. Confirmación Novia";
                temp[cant++] = "7. Copia DNI Testigo 1";
                temp[cant++] = "8. Copia DNI Testigo 2";
                temp[cant++] = "9. Copia DNI Padrinos";
            }

            string[] resultado = new string[cant];
            for (int i = 0; i < cant; i++)
            {
                resultado[i] = temp[i];
            }
            return resultado;
        }

        private void DesmarcarSoloRequisitosPadrino(string sacramento, CheckBox[] arregloChecks, bool desmarcarDniPadrino, bool desmarcarCertificadoEstadoCivil)
        {
            if (sacramento == "Bautizo")
            {
                if (desmarcarDniPadrino && arregloChecks.Length > 4 && arregloChecks[4] != null)
                {
                    arregloChecks[4].Checked = false;
                }
                if (desmarcarCertificadoEstadoCivil && arregloChecks.Length > 5 && arregloChecks[5] != null)
                {
                    arregloChecks[5].Checked = false;
                }
            }
            else if (sacramento == "Primera Comunión")
            {
                if (desmarcarDniPadrino && arregloChecks.Length > 4 && arregloChecks[4] != null)
                {
                    arregloChecks[4].Checked = false;
                }
            }
            else if (sacramento == "Confirmación")
            {
                if (desmarcarDniPadrino && arregloChecks.Length > 3 && arregloChecks[3] != null)
                {
                    arregloChecks[3].Checked = false;
                }
                if (desmarcarCertificadoEstadoCivil && arregloChecks.Length > 4 && arregloChecks[4] != null)
                {
                    arregloChecks[4].Checked = false;
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
            pbSigep.Location = new Point(130, 35);
            pbSigep.SizeMode = PictureBoxSizeMode.Zoom;
            string ruta_logo_sigep = Path.Combine(this.ruta_assets, "logo.png");
            if (File.Exists(ruta_logo_sigep))
            {
                pbSigep.Image = Image.FromFile(ruta_logo_sigep);
            }
            f_der.Controls.Add(pbSigep);

            PictureBox pbParr = new PictureBox();
            pbParr.Size = new Size(110, 110);
            pbParr.Location = new Point(260, 35);
            pbParr.SizeMode = PictureBoxSizeMode.Zoom;
            string ruta_logo_parroquia = Path.Combine(this.ruta_assets, "parroquia.png");
            if (File.Exists(ruta_logo_parroquia))
            {
                pbParr.Image = Image.FromFile(ruta_logo_parroquia);
            }
            f_der.Controls.Add(pbParr);

            Label lblBienv = new Label();
            lblBienv.Text = "Bienvenido a SIGEP";
            lblBienv.Font = FuentePx(28f, FontStyle.Bold);
            lblBienv.ForeColor = ColorTranslator.FromHtml("#0f172a");
            lblBienv.Size = new Size(500, 42);
            lblBienv.Location = new Point(0, 160);
            lblBienv.TextAlign = ContentAlignment.MiddleCenter;
            f_der.Controls.Add(lblBienv);

            Label lblSub = new Label();
            lblSub.Text = "Inicie sesión para continuar";
            lblSub.Font = FuentePx(15f, FontStyle.Regular);
            lblSub.ForeColor = ColorTranslator.FromHtml("#64748b");
            lblSub.Size = new Size(500, 28);
            lblSub.Location = new Point(0, 205);
            lblSub.TextAlign = ContentAlignment.MiddleCenter;
            f_der.Controls.Add(lblSub);

            Label lblU = new Label() { Text = "Usuario:", Font = FuentePx(13.5f, FontStyle.Bold), Location = new Point(90, 255), AutoSize = true };
            f_der.Controls.Add(lblU);

            TextBox txt_u = new TextBox() { Size = new Size(320, 32), Location = new Point(90, 280) };
            this.adjuntar_validacion(txt_u, "libre");
            txt_u.Font = FuentePx(15f, FontStyle.Regular);
            f_der.Controls.Add(txt_u);

            Label lblP = new Label() { Text = "Contraseña:", Font = FuentePx(13.5f, FontStyle.Bold), Location = new Point(90, 330), AutoSize = true };
            f_der.Controls.Add(lblP);

            TextBox txt_p = new TextBox() { Size = new Size(320, 32), Location = new Point(90, 355), PasswordChar = '*' };
            this.adjuntar_validacion(txt_p, "libre");
            txt_p.Font = FuentePx(15f, FontStyle.Regular);
            f_der.Controls.Add(txt_p);

            Button btn_ingresar = CrearBotonAccion("Ingresar al sistema", ColorTranslator.FromHtml("#2563eb"), new Point(90, 415), new Size(320, 45), 15f);
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
                    Math.Max(0, (this.container.ClientSize.Width - f_login.Width) / 2),
                    Math.Max(0, (this.container.ClientSize.Height - f_login.Height) / 2)
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
            this.sidebar.Width = 235;
            this.sidebar.Dock = DockStyle.Left;
            this.sidebar.BackColor = ColorTranslator.FromHtml("#1e293b");

            string ruta_logo_blanco = Path.Combine(this.ruta_assets, "logoblanco.png");
            if (File.Exists(ruta_logo_blanco))
            {
                PictureBox pbLogo = new PictureBox();
                pbLogo.Size = new Size(85, 85);
                pbLogo.Location = new Point(75, 28);
                pbLogo.SizeMode = PictureBoxSizeMode.Zoom;
                pbLogo.Image = Image.FromFile(ruta_logo_blanco);
                this.sidebar.Controls.Add(pbLogo);
            }

            Button btnDash = CrearBotonSidebar("📊 Panel de Inicio", 145);
            btnDash.Click += (s, e) => this.mostrar_dashboard();
            this.sidebar.Controls.Add(btnDash);

            Button btnReg = CrearBotonSidebar("📝 Nuevo Expediente", 195);
            btnReg.Click += (s, e) => this.mostrar_registro();
            this.sidebar.Controls.Add(btnReg);

            Button btnBus = CrearBotonSidebar("🔍 Buscar y Editar", 245);
            btnBus.Click += (s, e) => this.mostrar_buscar();
            this.sidebar.Controls.Add(btnBus);

            Button btnRep = CrearBotonSidebar("📑 Reporte de Expedientes", 300);
            btnRep.Click += (s, e) => this.mostrar_reportes();
            this.sidebar.Controls.Add(btnRep);

            Button btnConf = CrearBotonSidebar("⚙ Configuración de Fechas", 345);
            btnConf.Click += (s, e) => this.mostrar_configuracion();
            this.sidebar.Controls.Add(btnConf);

            Button btnAvanzadas = CrearBotonSidebar("📜 Opciones Avanzadas", 395);
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
            btn.Font = FuentePx(14f, FontStyle.Regular);
            btn.Size = new Size(235, 44);
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

            Label lblHola = new Label() { Text = "Hola, Bienvenida", Font = FuentePx(28f, FontStyle.Bold), ForeColor = ColorTranslator.FromHtml("#0f172a"), Location = new Point(30, 30), AutoSize = true };
            this.content_frame.Controls.Add(lblHola);

            Label lblSub = new Label() { Text = "Resumen de Operaciones - Parroquia San José", Font = FuentePx(15f, FontStyle.Regular), ForeColor = ColorTranslator.FromHtml("#64748b"), Location = new Point(30, lblHola.Bottom + 8), AutoSize = true };
            this.content_frame.Controls.Add(lblSub);

            Panel card1 = new Panel() { Size = new Size(360, 125), Location = new Point(30, lblSub.Bottom + 25), BackColor = Color.White, BorderStyle = BorderStyle.FixedSingle };
            Label lblCardTit = new Label() { Text = "EXPEDIENTES TOTALES", Font = FuentePx(13.5f, FontStyle.Bold), ForeColor = ColorTranslator.FromHtml("#10b981"), Location = new Point(20, 20), AutoSize = true };
            Label lblTotal = new Label() { Text = this.db.total_registros.ToString(), Font = FuentePx(34f, FontStyle.Bold), ForeColor = ColorTranslator.FromHtml("#0f172a"), Location = new Point(20, 52), AutoSize = true };

            card1.Controls.Add(lblCardTit);
            card1.Controls.Add(lblTotal);
            this.content_frame.Controls.Add(card1);
        }

        // ================= 4. CONFIGURACIÓN =================
        private void mostrar_configuracion()
        {
            this.limpiar_contenido();

            Label lblTit = new Label() { Text = "Configuración de Fechas de Catequesis", Font = FuentePx(24f, FontStyle.Bold), ForeColor = ColorTranslator.FromHtml("#2563eb"), Location = new Point(30, 25), AutoSize = true };
            this.content_frame.Controls.Add(lblTit);

            Panel f_conf = new Panel() { Size = new Size(680, 250), Location = new Point(30, lblTit.Bottom + 20), BackColor = Color.White, BorderStyle = BorderStyle.FixedSingle };
            Label lblSub = new Label() { Text = "Selecciona la fecha de inicio (Día / Mes)", Font = FuentePx(15f, FontStyle.Bold), Location = new Point(25, 22), AutoSize = true };
            f_conf.Controls.Add(lblSub);

            string[] lista_dias = new string[32];
            lista_dias[0] = "--";
            for (int i = 1; i <= 31; i++)
            {
                lista_dias[i] = i.ToString("D2");
            }

            string[] lista_meses = new string[13];
            lista_meses[0] = "--";
            for (int i = 1; i <= 12; i++)
            {
                lista_meses[i] = i.ToString("D2");
            }

            string[] partes_com = this.config.fecha_comunion.Split('/');
            string c_dia = partes_com.Length == 2 ? partes_com[0] : "--";
            string c_mes = partes_com.Length == 2 ? partes_com[1] : "--";

            Label lblCom = new Label() { Text = "Primera Comunión:", Font = FuentePx(14f, FontStyle.Bold), Location = new Point(25, 72), AutoSize = true };
            ComboBox cb_d_com = new ComboBox() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 75, Font = FuentePx(13.5f) };
            cb_d_com.Items.AddRange(lista_dias);
            cb_d_com.SelectedItem = c_dia;

            ComboBox cb_m_com = new ComboBox() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 75, Font = FuentePx(13.5f) };
            cb_m_com.Items.AddRange(lista_meses);
            cb_m_com.SelectedItem = c_mes;

            string[] partes_con = this.config.fecha_confirmacion.Split('/');
            string cf_dia = partes_con.Length == 2 ? partes_con[0] : "--";
            string cf_mes = partes_con.Length == 2 ? partes_con[1] : "--";

            Label lblCon = new Label() { Text = "Confirmación:", Font = FuentePx(14f, FontStyle.Bold), Location = new Point(25, 118), AutoSize = true };
            ComboBox cb_d_con = new ComboBox() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 75, Font = FuentePx(13.5f) };
            cb_d_con.Items.AddRange(lista_dias);
            cb_d_con.SelectedItem = cf_dia;

            ComboBox cb_m_con = new ComboBox() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 75, Font = FuentePx(13.5f) };
            cb_m_con.Items.AddRange(lista_meses);
            cb_m_con.SelectedItem = cf_mes;

            f_conf.Controls.AddRange(new Control[] { lblCom, cb_d_com, cb_m_com, lblCon, cb_d_con, cb_m_con });

            int xCombos = Math.Max(lblCom.Right, lblCon.Right) + 25;
            cb_d_com.Location = new Point(xCombos, 69);
            cb_m_com.Location = new Point(cb_d_com.Right + 12, 69);
            cb_d_con.Location = new Point(xCombos, 115);
            cb_m_con.Location = new Point(cb_d_con.Right + 12, 115);

            Button btnGuardar = CrearBotonAccion("Guardar Cambios", ColorTranslator.FromHtml("#10b981"), new Point(25, 175), new Size(190, 42));
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

            Label lblTit = new Label() { Text = "Registro Documentario", Font = FuentePx(24f, FontStyle.Bold), ForeColor = ColorTranslator.FromHtml("#2563eb"), Location = new Point(25, 15), AutoSize = true };
            this.content_frame.Controls.Add(lblTit);

            Panel f_main = new Panel() { Location = new Point(25, lblTit.Bottom + 12), Size = new Size(Math.Max(780, this.content_frame.ClientSize.Width - 55), 690), BackColor = Color.White, BorderStyle = BorderStyle.FixedSingle };

            Label lblTram = new Label() { Text = "Trámite:", Font = FuentePx(13.5f, FontStyle.Bold), Location = new Point(25, 22), AutoSize = true };
            ComboBox cb_tipo = new ComboBox() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 220, Font = FuentePx(13.5f) };
            cb_tipo.Items.AddRange(new string[] { "Bautizo", "Primera Comunión", "Confirmación", "Matrimonio" });

            Label lbl_d1 = new Label() { Text = "DNI Bautizado:", Font = FuentePx(13.5f, FontStyle.Bold), AutoSize = true };
            TextBox txt_d1 = new TextBox();
            this.adjuntar_validacion(txt_d1, "dni");

            Label lbl_nom1 = new Label() { Text = "Nombres:", Font = FuentePx(13.5f, FontStyle.Bold), AutoSize = true };
            TextBox txt_nom1 = new TextBox();
            AsignarPlaceholder(txt_nom1, "NOMBRES");
            this.adjuntar_validacion(txt_nom1, "texto");

            Label lbl_ape1 = new Label() { Text = "Apellidos:", Font = FuentePx(13.5f, FontStyle.Bold), AutoSize = true };
            TextBox txt_ape1 = new TextBox();
            AsignarPlaceholder(txt_ape1, "APELLIDOS");
            this.adjuntar_validacion(txt_ape1, "texto");

            Label lbl_d2 = new Label() { Text = "DNI Novia:", Font = FuentePx(13.5f, FontStyle.Bold), AutoSize = true, Visible = false };
            TextBox txt_d2 = new TextBox() { Visible = false };
            this.adjuntar_validacion(txt_d2, "dni");

            Label lbl_nom2 = new Label() { Text = "Nombres:", Font = FuentePx(13.5f, FontStyle.Bold), AutoSize = true, Visible = false };
            TextBox txt_nom2 = new TextBox() { Visible = false };
            AsignarPlaceholder(txt_nom2, "NOMBRES NOVIA");
            this.adjuntar_validacion(txt_nom2, "texto");

            Label lbl_ape2 = new Label() { Text = "Apellidos:", Font = FuentePx(13.5f, FontStyle.Bold), AutoSize = true, Visible = false };
            TextBox txt_ape2 = new TextBox() { Visible = false };
            AsignarPlaceholder(txt_ape2, "APELLIDOS NOVIA");
            this.adjuntar_validacion(txt_ape2, "texto");

            this.VincularAutocompletadoReniecSeparado(txt_d1, txt_nom1, txt_ape1, txt_d2, null);
            this.VincularAutocompletadoReniecSeparado(txt_d2, txt_nom2, txt_ape2, txt_d1, null);

            CheckBox chk_padrinos = new CheckBox() { Text = "¿Cuenta con Padrinos?", Font = FuentePx(13.5f, FontStyle.Bold), Location = new Point(25, 140), AutoSize = true, Checked = true };
            Label lbl_est_pad = new Label() { Text = "Estado Civil Padrino(s):", Font = FuentePx(13.5f, FontStyle.Bold), AutoSize = true };
            ComboBox cb_est_pad = new ComboBox() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 160, Font = FuentePx(13.5f) };
            cb_est_pad.Items.AddRange(new string[] { "Soltero(a)", "Casado(a)" });
            cb_est_pad.SelectedIndex = 0;

            Panel f_req = new Panel() { Location = new Point(25, 178), Size = new Size(f_main.ClientSize.Width - 50, 185), BackColor = ColorTranslator.FromHtml("#f8fafc"), BorderStyle = BorderStyle.FixedSingle };
            Label lblReqTit = new Label() { Text = "Requisitos de Documentación:", Font = FuentePx(13.5f, FontStyle.Bold), Location = new Point(15, 10), AutoSize = true };
            f_req.Controls.Add(lblReqTit);

            for (int i = 0; i < 9; i++)
            {
                CheckBox chk = new CheckBox() { Font = FuentePx(13f, FontStyle.Regular), AutoSize = true };
                this.chks[i] = chk;
                f_req.Controls.Add(chk);
            }

            Label lblPago = new Label() { Text = "Estado de Pago:", Font = FuentePx(13.5f, FontStyle.Bold), Location = new Point(25, 378), AutoSize = true };
            ComboBox cb_pago = new ComboBox() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 150, Font = FuentePx(13.5f) };
            cb_pago.Items.AddRange(new string[] { "Pagado", "Falta Pagar" });
            cb_pago.SelectedIndex = 0;

            TextBox txt_deuda = new TextBox() { Width = 150, Visible = false };
            AsignarPlaceholder(txt_deuda, "Monto deuda S/");
            this.adjuntar_validacion(txt_deuda, "numero");

            Panel f_obs_izq = new Panel() { BackColor = ColorTranslator.FromHtml("#f1f5f9"), BorderStyle = BorderStyle.FixedSingle };
            Label lblObsGen = new Label() { Text = "Observación Generada:", Font = FuentePx(13.5f, FontStyle.Bold), Location = new Point(15, 10), AutoSize = true };
            TextBox txt_obs_auto = new TextBox() { Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical, Location = new Point(15, 36), BackColor = Color.White, Font = FuentePx(13f) };
            f_obs_izq.Controls.AddRange(new Control[] { lblObsGen, txt_obs_auto });

            Panel f_obs_der = new Panel() { BackColor = ColorTranslator.FromHtml("#f1f5f9"), BorderStyle = BorderStyle.FixedSingle };
            CheckBox chk_adic = new CheckBox() { Text = "¿Añadir observación adicional manual?", Font = FuentePx(13.5f, FontStyle.Bold), Location = new Point(15, 10), AutoSize = true };
            TextBox txt_obs_adic = new TextBox() { Multiline = true, ScrollBars = ScrollBars.Vertical, Location = new Point(15, 36), Visible = false, Font = FuentePx(13f) };
            f_obs_der.Controls.AddRange(new Control[] { chk_adic, txt_obs_adic });

            Button btnGuardarExp = CrearBotonAccion("Inscribir y Guardar Expediente", ColorTranslator.FromHtml("#2563eb"), new Point(250, 615), new Size(340, 48), 16f);

            f_main.Controls.AddRange(new Control[] {
                lblTram, cb_tipo,
                lbl_d1, txt_d1, lbl_nom1, txt_nom1, lbl_ape1, txt_ape1,
                lbl_d2, txt_d2, lbl_nom2, txt_nom2, lbl_ape2, txt_ape2,
                chk_padrinos, lbl_est_pad, cb_est_pad,
                f_req, lblPago, cb_pago, txt_deuda,
                f_obs_izq, f_obs_der, btnGuardarExp
            });

            Action ajustarDisenoRegistro = () =>
            {
                int anchoDisp = Math.Max(780, this.content_frame.ClientSize.Width - 55);
                f_main.Width = anchoDisp;
                cb_tipo.Location = new Point(lblTram.Right + 12, 18);

                this.DistribuirFilaPersona(f_main, 58, true, lbl_d1, txt_d1, lbl_nom1, txt_nom1, lbl_ape1, txt_ape1);
                this.DistribuirFilaPersona(f_main, 98, true, lbl_d2, txt_d2, lbl_nom2, txt_nom2, lbl_ape2, txt_ape2);

                chk_padrinos.Location = new Point(25, 140);
                lbl_est_pad.Location = new Point(chk_padrinos.Right + 30, 142);
                cb_est_pad.Location = new Point(lbl_est_pad.Right + 10, 138);

                f_req.Width = anchoDisp - 50;
                int mitadReq = Math.Max(340, (f_req.ClientSize.Width - 40) / 2);
                for (int i = 0; i < 9; i++)
                {
                    this.chks[i].Location = new Point(20 + ((i % 2) * mitadReq), 38 + ((i / 2) * 28));
                }

                lblPago.Location = new Point(25, 380);
                cb_pago.Location = new Point(lblPago.Right + 12, 376);
                txt_deuda.Location = new Point(cb_pago.Right + 16, 376);

                int anchoCajaObs = (f_req.Width - 20) / 2;
                f_obs_izq.Location = new Point(25, 420);
                f_obs_izq.Size = new Size(anchoCajaObs, 175);
                txt_obs_auto.Size = new Size(anchoCajaObs - 30, 122);

                f_obs_der.Location = new Point(f_obs_izq.Right + 20, 420);
                f_obs_der.Size = new Size(anchoCajaObs, 175);
                txt_obs_adic.Size = new Size(anchoCajaObs - 30, 122);

                btnGuardarExp.Location = new Point((anchoDisp - btnGuardarExp.Width) / 2, 615);
                f_main.Invalidate();
            };

            this.content_frame.Resize += (s, e) => ajustarDisenoRegistro();

            Action actualizar_obs_auto = () =>
            {
                string[] faltantes = new string[9];
                int cantFaltantes = 0;
                for (int i = 0; i < this.reqs_activos; i++)
                {
                    if (!this.chks[i].Checked)
                    {
                        faltantes[cantFaltantes++] = this.chks[i].Text.Substring(3);
                    }
                }

                string texto = (cantFaltantes > 0)
                    ? "Faltan los siguientes documentos físicos:\r\n- " + this.UnirArregloConSeparador(faltantes, cantFaltantes, "\r\n- ") + "\r\n"
                    : "Documentación física completa.\r\n";

                if (cb_pago.SelectedItem != null && cb_pago.SelectedItem.ToString() == "Falta Pagar")
                {
                    texto += "Pendiente de pago por regularizar.";
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
                bool esMat = (eleccion == "Matrimonio");

                lbl_d2.Visible = esMat;
                txt_d2.Visible = esMat;
                lbl_nom2.Visible = esMat;
                txt_nom2.Visible = esMat;
                lbl_ape2.Visible = esMat;
                txt_ape2.Visible = esMat;

                chk_padrinos.Visible = !esMat;
                lbl_est_pad.Visible = ((eleccion == "Bautizo" || eleccion == "Confirmación") && tienePad);
                cb_est_pad.Visible = ((eleccion == "Bautizo" || eleccion == "Confirmación") && tienePad);

                string[] reqs = this.ObtenerRequisitosSacramento(eleccion, tienePad, estPad);
                this.reqs_activos = reqs.Length;
                for (int j = 0; j < this.chks.Length; j++)
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
                ajustarDisenoRegistro();
                actualizar_obs_auto();
            };

            cb_tipo.SelectedIndexChanged += (s, e) =>
            {
                for (int j = 0; j < this.chks.Length; j++)
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
                txt_obs_adic.Visible = chk_adic.Checked;
                if (!chk_adic.Checked)
                {
                    txt_obs_adic.Text = "";
                }
            };

            this.content_frame.Controls.Add(f_main);
            cb_tipo.SelectedItem = "Bautizo";
            ajustarDisenoRegistro();

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

                string[] errores = new string[20];
                int cantErrores = 0;

                if (dni1_val.Length != 8 || dni1_val == "00000000")
                {
                    errores[cantErrores++] = "- El " + this.ObtenerEtiquetaDniPorSacramento(tipo_sel).Replace(":", "") + " debe tener 8 dígitos numéricos válidos.";
                }
                if (!this.EsNombreValido(nom1_val))
                {
                    errores[cantErrores++] = "- Los Nombres del titular deben contener al menos 2 letras válidas.";
                }
                if (!this.EsNombreValido(ape1_val))
                {
                    errores[cantErrores++] = "- Los Apellidos del titular deben contener al menos 2 letras válidas.";
                }

                if (tipo_sel == "Matrimonio")
                {
                    dni2_val = txt_d2.Text.Trim();
                    nom2_val = Regex.Replace(BaseDatos.A_MayusculasConTildes(txt_nom2.Text), @"\s+", " ");
                    ape2_val = Regex.Replace(BaseDatos.A_MayusculasConTildes(txt_ape2.Text), @"\s+", " ");

                    if (dni2_val.Length != 8 || dni2_val == "00000000")
                    {
                        errores[cantErrores++] = "- El DNI Novia debe tener 8 dígitos numéricos válidos.";
                    }
                    if (!this.EsNombreValido(nom2_val))
                    {
                        errores[cantErrores++] = "- Los Nombres de la Novia deben contener al menos 2 letras válidas.";
                    }
                    if (!this.EsNombreValido(ape2_val))
                    {
                        errores[cantErrores++] = "- Los Apellidos de la Novia deben contener al menos 2 letras válidas.";
                    }
                    if (dni1_val != "" && dni1_val == dni2_val)
                    {
                        errores[cantErrores++] = "- No se pueden colocar 2 DNIs iguales. Los DNIs de los novios deben ser diferentes.";
                    }
                    if (nom1_val != "" && ape1_val != "" && (nom1_val + " " + ape1_val) == (nom2_val + " " + ape2_val))
                    {
                        errores[cantErrores++] = "- Los nombres y apellidos de los novios no pueden ser iguales.";
                    }
                }

                for (int idxExist = 0; idxExist < this.db.total_registros; idxExist++)
                {
                    if (this.db.tipos[idxExist] == tipo_sel)
                    {
                        if (this.db.dnis_prin[idxExist] == dni1_val || (tipo_sel == "Matrimonio" && this.db.dnis_sec[idxExist] == dni1_val))
                        {
                            errores[cantErrores++] = "- El DNI " + dni1_val + " ya se encuentra registrado en " + tipo_sel + ".";
                            break;
                        }
                        if (tipo_sel == "Matrimonio" && (this.db.dnis_prin[idxExist] == dni2_val || this.db.dnis_sec[idxExist] == dni2_val))
                        {
                            errores[cantErrores++] = "- El DNI " + dni2_val + " ya se encuentra registrado en " + tipo_sel + ".";
                            break;
                        }
                    }
                }

                if (tipo_sel == "Primera Comunión" && this.config.fecha_comunion == "--/--")
                {
                    errores[cantErrores++] = "- Debe configurar la fecha de inicio de Catequesis en 'Configuración de Fechas' antes de registrar.";
                }
                if (tipo_sel == "Confirmación" && this.config.fecha_confirmacion == "--/--")
                {
                    errores[cantErrores++] = "- Debe configurar la fecha de inicio de Catequesis en 'Configuración de Fechas' antes de registrar.";
                }

                string pago = cb_pago.SelectedItem.ToString();
                string deuda = "0";
                if (pago == "Falta Pagar")
                {
                    deuda = txt_deuda.Text.Trim();
                    double monto_float = 0;
                    if (deuda == "" || !double.TryParse(deuda, NumberStyles.Any, CultureInfo.InvariantCulture, out monto_float) || monto_float <= 0)
                    {
                        errores[cantErrores++] = "- Debe especificar un monto de deuda válido mayor a 0.";
                    }
                }

                string obs_adic_txt = "";
                if (chk_adic.Checked)
                {
                    obs_adic_txt = txt_obs_adic.Text.Trim();
                    if (obs_adic_txt == "")
                    {
                        errores[cantErrores++] = "- Ha marcado añadir observación adicional manual, pero la caja está vacía.";
                    }
                }

                if (cantErrores > 0)
                {
                    MessageBox.Show("Se detectaron los siguientes errores:\n\n" + this.UnirArregloConSeparador(errores, cantErrores, "\n"), "Datos Incompletos o Inválidos", MessageBoxButtons.OK, MessageBoxIcon.Error);
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

                string[] arr_chks = new string[this.reqs_activos];
                for (int k = 0; k < this.reqs_activos; k++)
                {
                    arr_chks[k] = this.chks[k].Checked ? "1" : "0";
                }
                string chks_str = this.UnirArregloConSeparador(arr_chks, this.reqs_activos, "-");

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
        }

        // ================= 6. BUSCAR Y EDITAR =================
        private void mostrar_buscar()
        {
            this.limpiar_contenido();

            Label lblTit = new Label() { Text = "Buscador y Editor de Expedientes", Font = FuentePx(24f, FontStyle.Bold), ForeColor = ColorTranslator.FromHtml("#2563eb"), Location = new Point(25, 15), AutoSize = true };
            this.content_frame.Controls.Add(lblTit);

            Panel f_top = new Panel() { Location = new Point(25, lblTit.Bottom + 10), Size = new Size(Math.Max(780, this.content_frame.ClientSize.Width - 55), 60), BackColor = Color.White, BorderStyle = BorderStyle.FixedSingle };

            Label lblCriterio = new Label() { Text = "Buscar por:", Font = FuentePx(13.5f, FontStyle.Bold), Location = new Point(15, 20), AutoSize = true };
            ComboBox cb_criterio = new ComboBox() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 130, Font = FuentePx(13.5f) };
            cb_criterio.Items.AddRange(new string[] { "DNI", "Apellidos" });
            cb_criterio.SelectedIndex = 0;

            TextBox txt_bus_dni = new TextBox() { Width = 220 };
            AsignarPlaceholder(txt_bus_dni, "Ingrese 8 dígitos del DNI");
            this.adjuntar_validacion(txt_bus_dni, "dni");

            TextBox txt_bus_ape = new TextBox() { Width = 250, Visible = false };
            AsignarPlaceholder(txt_bus_ape, "INGRESE APELLIDOS A BUSCAR");
            this.adjuntar_validacion(txt_bus_ape, "texto");

            Button btnConsultar = CrearBotonAccion("Consultar", ColorTranslator.FromHtml("#10b981"), new Point(520, 12), new Size(140, 35));

            f_top.Controls.AddRange(new Control[] { lblCriterio, cb_criterio, txt_bus_dni, txt_bus_ape, btnConsultar });
            this.content_frame.Controls.Add(f_top);

            DataGridView tablaResultados = new DataGridView() { Location = new Point(25, f_top.Bottom + 10), Size = new Size(f_top.Width, 145) };
            this.ConfigurarTablaResponsiva(tablaResultados);
            tablaResultados.Columns.Add("colDni", "DNI");
            tablaResultados.Columns.Add("colNom", "Nombres");
            tablaResultados.Columns.Add("colApe", "Apellidos");
            tablaResultados.Columns.Add("colSac", "Sacramento");
            tablaResultados.Columns.Add("colEst", "Estado");
            tablaResultados.Columns.Add("colPag", "Pago");
            this.content_frame.Controls.Add(tablaResultados);

            Panel f_edit = new Panel() { Location = new Point(25, tablaResultados.Bottom + 10), Size = new Size(f_top.Width, 650), BackColor = Color.White, BorderStyle = BorderStyle.FixedSingle, Visible = false };

            int idx_actual = -1;
            bool cargandoEdicion = false;
            bool llenandoTabla = false;

            Label lbl_e_dni1 = new Label() { Text = "DNI Bautizado:", Font = FuentePx(13.5f, FontStyle.Bold), AutoSize = true };
            TextBox t_dni1 = new TextBox();
            this.adjuntar_validacion(t_dni1, "dni");

            Label lbl_e_nom1 = new Label() { Text = "Nombres:", Font = FuentePx(13.5f, FontStyle.Bold), AutoSize = true };
            TextBox t_nom1 = new TextBox();
            this.adjuntar_validacion(t_nom1, "texto");

            Label lbl_e_ape1 = new Label() { Text = "Apellidos:", Font = FuentePx(13.5f, FontStyle.Bold), AutoSize = true };
            TextBox t_ape1 = new TextBox();
            this.adjuntar_validacion(t_ape1, "texto");

            Label lbl_e_dni2 = new Label() { Text = "DNI Novia:", Font = FuentePx(13.5f, FontStyle.Bold), AutoSize = true, Visible = false };
            TextBox t_dni2 = new TextBox() { Visible = false };
            this.adjuntar_validacion(t_dni2, "dni");

            Label lbl_e_nom2 = new Label() { Text = "Nombres:", Font = FuentePx(13.5f, FontStyle.Bold), AutoSize = true, Visible = false };
            TextBox t_nom2 = new TextBox() { Visible = false };
            this.adjuntar_validacion(t_nom2, "texto");

            Label lbl_e_ape2 = new Label() { Text = "Apellidos:", Font = FuentePx(13.5f, FontStyle.Bold), AutoSize = true, Visible = false };
            TextBox t_ape2 = new TextBox() { Visible = false };
            this.adjuntar_validacion(t_ape2, "texto");

            this.VincularAutocompletadoReniecSeparado(t_dni1, t_nom1, t_ape1, t_dni2, () => cargandoEdicion);
            this.VincularAutocompletadoReniecSeparado(t_dni2, t_nom2, t_ape2, t_dni1, () => cargandoEdicion);

            Label lbl_e_est = new Label() { Text = "Estado:", Font = FuentePx(13.5f, FontStyle.Bold), AutoSize = true };
            ComboBox c_est = new ComboBox() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 130, Font = FuentePx(13.5f) };
            c_est.Items.AddRange(new string[] { "Inscrito(a)", "Observado", "Finalizado" });

            Label lbl_e_pag = new Label() { Text = "Pago:", Font = FuentePx(13.5f, FontStyle.Bold), AutoSize = true };
            ComboBox c_pag = new ComboBox() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 120, Font = FuentePx(13.5f) };
            c_pag.Items.AddRange(new string[] { "Pagado", "Falta Pagar" });

            TextBox t_deu = new TextBox() { Width = 85 };
            AsignarPlaceholder(t_deu, "Deuda S/");
            this.adjuntar_validacion(t_deu, "numero");

            CheckBox chk_e_padrinos = new CheckBox() { Text = "¿Con Padrinos?", Font = FuentePx(13f, FontStyle.Bold), AutoSize = true };
            Label lbl_e_est_pad = new Label() { Text = "Est. Civil:", Font = FuentePx(13f, FontStyle.Bold), AutoSize = true };
            ComboBox cb_e_est_pad = new ComboBox() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 130, Font = FuentePx(13f) };
            cb_e_est_pad.Items.AddRange(new string[] { "Soltero(a)", "Casado(a)" });
            cb_e_est_pad.SelectedIndex = 0;

            Panel f_req_edit = new Panel() { Location = new Point(25, 135), Size = new Size(f_edit.ClientSize.Width - 50, 178), BackColor = ColorTranslator.FromHtml("#f8fafc"), BorderStyle = BorderStyle.FixedSingle };
            Label lblReqEditTit = new Label() { Text = "Requisitos de Documentación:", Font = FuentePx(13.5f, FontStyle.Bold), Location = new Point(15, 8), AutoSize = true };
            f_req_edit.Controls.Add(lblReqEditTit);

            for (int i = 0; i < 9; i++)
            {
                CheckBox chk = new CheckBox() { Font = FuentePx(13f, FontStyle.Regular), AutoSize = true };
                this.chks_edit[i] = chk;
                f_req_edit.Controls.Add(chk);
            }

            Panel f_obs_izq_edit = new Panel() { BackColor = ColorTranslator.FromHtml("#f1f5f9"), BorderStyle = BorderStyle.FixedSingle };
            Label lblObsGenE = new Label() { Text = "Observación Generada:", Font = FuentePx(13.5f, FontStyle.Bold), Location = new Point(15, 8), AutoSize = true };
            TextBox txt_obs_auto_edit = new TextBox() { Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical, Location = new Point(15, 32), BackColor = Color.White, Font = FuentePx(13f) };
            f_obs_izq_edit.Controls.AddRange(new Control[] { lblObsGenE, txt_obs_auto_edit });

            Panel f_obs_der_edit = new Panel() { BackColor = ColorTranslator.FromHtml("#f1f5f9"), BorderStyle = BorderStyle.FixedSingle };
            CheckBox chk_adic_edit = new CheckBox() { Text = "¿Añadir/Ver observación adicional manual?", Font = FuentePx(13f, FontStyle.Bold), Location = new Point(15, 8), AutoSize = true };
            TextBox txt_obs_adic_edit = new TextBox() { Multiline = true, ScrollBars = ScrollBars.Vertical, Location = new Point(15, 32), Visible = false, Font = FuentePx(13f) };
            f_obs_der_edit.Controls.AddRange(new Control[] { chk_adic_edit, txt_obs_adic_edit });

            Label lblFecOf = new Label() { Text = "Fecha Oficial:", Font = FuentePx(13.5f, FontStyle.Bold), AutoSize = true };
            Label lblFecMod = new Label() { Text = "(DD/MM/AAAA)", Font = FuentePx(11.5f, FontStyle.Regular), ForeColor = ColorTranslator.FromHtml("#64748b"), AutoSize = true };
            TextBox t_fec = new TextBox() { Width = 125 };
            this.adjuntar_validacion(t_fec, "fecha");

            Label lblLib = new Label() { Text = "Libro:", Font = FuentePx(13.5f, FontStyle.Bold), AutoSize = true };
            TextBox t_l = new TextBox() { Width = 70 };
            this.adjuntar_validacion(t_l, "registro_libro");

            Label lblFol = new Label() { Text = "Folio:", Font = FuentePx(13.5f, FontStyle.Bold), AutoSize = true };
            TextBox t_f = new TextBox() { Width = 70 };
            this.adjuntar_validacion(t_f, "registro_libro");

            Label lblAsi = new Label() { Text = "Asiento:", Font = FuentePx(13.5f, FontStyle.Bold), AutoSize = true };
            TextBox t_a = new TextBox() { Width = 70 };
            this.adjuntar_validacion(t_a, "registro_libro");

            Button btn_act = CrearBotonAccion("Actualizar Datos", ColorTranslator.FromHtml("#2563eb"), new Point(220, 560), new Size(210, 44), 14.5f);
            Button btn_eli = CrearBotonAccion("Eliminar Expediente", ColorTranslator.FromHtml("#ef4444"), new Point(460, 560), new Size(210, 44), 14.5f);

            f_edit.Controls.AddRange(new Control[] {
                lbl_e_dni1, t_dni1, lbl_e_nom1, t_nom1, lbl_e_ape1, t_ape1,
                lbl_e_dni2, t_dni2, lbl_e_nom2, t_nom2, lbl_e_ape2, t_ape2,
                lbl_e_est, c_est, lbl_e_pag, c_pag, t_deu, chk_e_padrinos, lbl_e_est_pad, cb_e_est_pad,
                f_req_edit, f_obs_izq_edit, f_obs_der_edit,
                lblFecOf, lblFecMod, t_fec, lblLib, t_l, lblFol, t_f, lblAsi, t_a,
                btn_act, btn_eli
            });
            this.content_frame.Controls.Add(f_edit);

            Action ajustarDisenoBuscar = () =>
            {
                int anchoDisp = Math.Max(780, this.content_frame.ClientSize.Width - 55);
                f_top.Width = anchoDisp;
                tablaResultados.Width = anchoDisp;
                f_edit.Width = anchoDisp;

                cb_criterio.Location = new Point(lblCriterio.Right + 10, 16);
                txt_bus_dni.Location = new Point(cb_criterio.Right + 15, 16);
                txt_bus_ape.Location = new Point(cb_criterio.Right + 15, 16);
                int derechaCaja = txt_bus_dni.Visible ? txt_bus_dni.Right : txt_bus_ape.Right;
                btnConsultar.Location = new Point(derechaCaja + 18, 12);

                this.DistribuirFilaPersona(f_edit, 16, true, lbl_e_dni1, t_dni1, lbl_e_nom1, t_nom1, lbl_e_ape1, t_ape1);
                this.DistribuirFilaPersona(f_edit, 54, true, lbl_e_dni2, t_dni2, lbl_e_nom2, t_nom2, lbl_e_ape2, t_ape2);

                lbl_e_est.Location = new Point(25, 97);
                c_est.Location = new Point(lbl_e_est.Right + 6, 93);
                lbl_e_pag.Location = new Point(c_est.Right + 16, 97);
                c_pag.Location = new Point(lbl_e_pag.Right + 6, 93);
                t_deu.Location = new Point(c_pag.Right + 10, 93);

                int xPad = t_deu.Visible ? (t_deu.Right + 18) : (c_pag.Right + 20);
                chk_e_padrinos.Location = new Point(xPad, 95);
                lbl_e_est_pad.Location = new Point(chk_e_padrinos.Right + 14, 97);
                cb_e_est_pad.Location = new Point(lbl_e_est_pad.Right + 6, 93);

                f_req_edit.Width = anchoDisp - 50;
                int mitadReq = Math.Max(340, (f_req_edit.ClientSize.Width - 40) / 2);
                for (int i = 0; i < 9; i++)
                {
                    this.chks_edit[i].Location = new Point(20 + ((i % 2) * mitadReq), 34 + ((i / 2) * 27));
                }

                int anchoCajaObs = (f_req_edit.Width - 20) / 2;
                f_obs_izq_edit.Location = new Point(25, 323);
                f_obs_izq_edit.Size = new Size(anchoCajaObs, 160);
                txt_obs_auto_edit.Size = new Size(anchoCajaObs - 30, 112);

                f_obs_der_edit.Location = new Point(f_obs_izq_edit.Right + 20, 323);
                f_obs_der_edit.Size = new Size(anchoCajaObs, 160);
                txt_obs_adic_edit.Size = new Size(anchoCajaObs - 30, 112);

                lblFecOf.Location = new Point(25, 495);
                lblFecMod.Location = new Point(25, lblFecOf.Bottom + 1);
                int xFec = Math.Max(lblFecOf.Right, lblFecMod.Right) + 12;
                t_fec.Location = new Point(xFec, 502);

                lblLib.Location = new Point(t_fec.Right + 25, 505);
                t_l.Location = new Point(lblLib.Right + 8, 502);

                lblFol.Location = new Point(t_l.Right + 22, 505);
                t_f.Location = new Point(lblFol.Right + 8, 502);

                lblAsi.Location = new Point(t_f.Right + 22, 505);
                t_a.Location = new Point(lblAsi.Right + 8, 502);

                int xBotones = (anchoDisp - 445) / 2;
                btn_act.Location = new Point(xBotones, 560);
                btn_eli.Location = new Point(btn_act.Right + 25, 560);

                f_top.Invalidate();
                f_edit.Invalidate();
            };

            this.content_frame.Resize += (s, e) => ajustarDisenoBuscar();

            cb_criterio.SelectedIndexChanged += (s, e) =>
            {
                bool esDni = (cb_criterio.SelectedItem.ToString() == "DNI");
                txt_bus_dni.Visible = esDni;
                txt_bus_ape.Visible = !esDni;
                txt_bus_dni.Text = "";
                txt_bus_ape.Text = "";
                ajustarDisenoBuscar();
            };

            Action actualizar_obs_auto_edit = () =>
            {
                if (cargandoEdicion)
                {
                    return;
                }
                string[] faltantes = new string[9];
                int cantFaltantes = 0;

                for (int j = 0; j < this.reqs_activos_edit; j++)
                {
                    if (!this.chks_edit[j].Checked)
                    {
                        faltantes[cantFaltantes++] = this.chks_edit[j].Text.Substring(3);
                    }
                }

                string texto = (cantFaltantes > 0)
                    ? "Faltan los siguientes documentos físicos:\r\n- " + this.UnirArregloConSeparador(faltantes, cantFaltantes, "\r\n- ") + "\r\n"
                    : "Documentación física completa.\r\n";

                if (c_pag.SelectedItem != null && c_pag.SelectedItem.ToString() == "Falta Pagar")
                {
                    texto += "Pendiente de pago por regularizar.";
                }
                txt_obs_auto_edit.Text = texto.Trim();

                if (cantFaltantes == 0 && c_pag.SelectedItem != null && c_pag.SelectedItem.ToString() == "Pagado")
                {
                    if (c_est.SelectedItem != null && c_est.SelectedItem.ToString() == "Observado")
                    {
                        c_est.SelectedItem = "Inscrito(a)";
                    }
                }

                if (cantFaltantes > 0 || (c_pag.SelectedItem != null && c_pag.SelectedItem.ToString() == "Falta Pagar"))
                {
                    if (c_est.SelectedItem != null && c_est.SelectedItem.ToString() == "Inscrito(a)")
                    {
                        c_est.SelectedItem = "Observado";
                    }
                }
            };

            Action reconstruir_requisitos_edicion = () =>
            {
                if (idx_actual < 0 || idx_actual >= this.db.total_registros)
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
                for (int k = 0; k < this.chks_edit.Length; k++)
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
                ajustarDisenoBuscar();
                actualizar_obs_auto_edit();
            };

            for (int i = 0; i < 9; i++)
            {
                this.chks_edit[i].CheckedChanged += (s, e) => actualizar_obs_auto_edit();
            }

            chk_e_padrinos.CheckedChanged += (s, e) =>
            {
                if (!cargandoEdicion && idx_actual >= 0 && idx_actual < this.db.total_registros)
                {
                    this.DesmarcarSoloRequisitosPadrino(this.db.tipos[idx_actual], this.chks_edit, true, true);
                    reconstruir_requisitos_edicion();
                }
            };

            cb_e_est_pad.SelectedIndexChanged += (s, e) =>
            {
                if (!cargandoEdicion && idx_actual >= 0 && idx_actual < this.db.total_registros)
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
                ajustarDisenoBuscar();
                actualizar_obs_auto_edit();
            };

            chk_adic_edit.CheckedChanged += (s, e) =>
            {
                txt_obs_adic_edit.Visible = chk_adic_edit.Checked;
                if (!chk_adic_edit.Checked)
                {
                    txt_obs_adic_edit.Text = "";
                }
            };

            Action<int> cargarExpedienteEnEditor = (idx) =>
            {
                if (idx < 0 || idx >= this.db.total_registros)
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
                cb_e_est_pad.SelectedItem = (this.db.estado_padrinos[idx] == "Casado(a)") ? "Casado(a)" : "Soltero(a)";

                reconstruir_requisitos_edicion();

                string[] memoria = this.db.checks_mem[idx].Split(new char[] { '-', ',' }, StringSplitOptions.RemoveEmptyEntries);
                for (int k = 0; k < this.reqs_activos_edit; k++)
                {
                    this.chks_edit[k].Checked = (k < memoria.Length && memoria[k] == "1");
                }

                c_est.SelectedItem = c_est.Items.Contains(this.db.estados[idx]) ? this.db.estados[idx] : c_est.Items[0];
                c_pag.SelectedItem = c_pag.Items.Contains(this.db.pagos[idx]) ? this.db.pagos[idx] : c_pag.Items[0];

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
                ajustarDisenoBuscar();
                actualizar_obs_auto_edit();
            };

            Action<int[], bool> poblarTablaResultados = (indices, mostrarMensaje) =>
            {
                llenandoTabla = true;
                tablaResultados.Rows.Clear();
                for (int r = 0; r < indices.Length; r++)
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

                    int filaIdx = tablaResultados.Rows.Add(dniMostrar, nomMostrar, apeMostrar, this.db.tipos[idx], this.db.estados[idx], this.db.pagos[idx]);
                    tablaResultados.Rows[filaIdx].Tag = idx;
                }
                llenandoTabla = false;

                if (indices.Length > 0)
                {
                    tablaResultados.Rows[0].Selected = true;
                    cargarExpedienteEnEditor(indices[0]);
                    if (mostrarMensaje)
                    {
                        MessageBox.Show("Se encontraron " + indices.Length + " expediente(s).", "Búsqueda Exitosa", MessageBoxButtons.OK, MessageBoxIcon.Information);
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
                    cargarExpedienteEnEditor((int)tablaResultados.Rows[e.RowIndex].Tag);
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
                    int[] res = this.db.buscar_indices_por_dni(dni_val, true);
                    if (res.Length == 0)
                    {
                        int[] resAntiguo = this.db.buscar_indices_por_dni(dni_val, false);
                        if (resAntiguo.Length > 0)
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
                    int[] res = this.db.buscar_indices_por_apellidos(ape_val, true);
                    if (res.Length == 0)
                    {
                        int[] resAntiguo = this.db.buscar_indices_por_apellidos(ape_val, false);
                        if (resAntiguo.Length > 0)
                        {
                            MessageBox.Show("El apellido '" + ape_val + "' corresponde a una Constancia Antigua registrada en 'Opciones Avanzadas'.", "Constancia Antigua", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            return;
                        }
                    }
                    poblarTablaResultados(res, conAlerta);
                }
            };

            btnConsultar.Click += (s, e) => ejecutarBusqueda(true);
            txt_bus_dni.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; ejecutarBusqueda(true); } };
            txt_bus_ape.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; ejecutarBusqueda(true); } };

            ajustarDisenoBuscar();

            btn_act.Click += (s, e) =>
            {
                if (idx_actual < 0 || idx_actual >= this.db.total_registros)
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
                if (!this.EsNombreValido(nom1_val) || !this.EsNombreValido(ape1_val))
                {
                    MessageBox.Show("Los Nombres y Apellidos del titular deben contener al menos 2 letras válidas.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
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

                for (int idxExist = 0; idxExist < this.db.total_registros; idxExist++)
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

                string[] arr_chks_e = new string[this.reqs_activos_edit];
                for (int j = 0; j < this.reqs_activos_edit; j++)
                {
                    arr_chks_e[j] = this.chks_edit[j].Checked ? "1" : "0";
                }
                string chks_str = this.UnirArregloConSeparador(arr_chks_e, this.reqs_activos_edit, "-");
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
                if (idx_actual < 0 || idx_actual >= this.db.total_registros)
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

            Label lblTit = new Label() { Text = "Reporte General de Expedientes", Font = FuentePx(24f, FontStyle.Bold), ForeColor = ColorTranslator.FromHtml("#2563eb"), Location = new Point(25, 15), AutoSize = true };
            this.content_frame.Controls.Add(lblTit);

            Panel f_filtros = new Panel() { Location = new Point(25, lblTit.Bottom + 10), Size = new Size(Math.Max(780, this.content_frame.ClientSize.Width - 55), 66), BackColor = ColorTranslator.FromHtml("#f1f5f9"), BorderStyle = BorderStyle.FixedSingle };

            Label lblSac = new Label() { Text = "Sacramento:", Font = FuentePx(13.5f, FontStyle.Bold), Location = new Point(15, 22), AutoSize = true };
            ComboBox cb_filtro_sac = new ComboBox() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 145, Font = FuentePx(13.5f) };
            cb_filtro_sac.Items.AddRange(new string[] { "Todos", "Bautizo", "Primera Comunión", "Confirmación", "Matrimonio" });
            cb_filtro_sac.SelectedItem = "Todos";

            Label lblFecOf = new Label() { Text = "Fecha Oficial:\n(DD/MM/AAAA)", Font = FuentePx(12.5f, FontStyle.Bold), AutoSize = true };
            TextBox txt_filtro_fec = new TextBox() { Width = 105 };
            this.adjuntar_validacion(txt_filtro_fec, "fecha");

            Label lblOrd = new Label() { Text = "Ordenar:", Font = FuentePx(13.5f, FontStyle.Bold), AutoSize = true };
            ComboBox cb_orden = new ComboBox() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 145, Font = FuentePx(13.5f) };
            cb_orden.Items.AddRange(new string[] { "Ascendente (A-Z)", "Descendente (Z-A)" });
            cb_orden.SelectedItem = "Ascendente (A-Z)";

            Button btnFiltrar = CrearBotonAccion("Aplicar Filtros", ColorTranslator.FromHtml("#2563eb"), new Point(740, 14), new Size(135, 36));

            f_filtros.Controls.AddRange(new Control[] { lblSac, cb_filtro_sac, lblFecOf, txt_filtro_fec, lblOrd, cb_orden, btnFiltrar });
            this.content_frame.Controls.Add(f_filtros);

            DataGridView tabla = new DataGridView() { Location = new Point(25, f_filtros.Bottom + 12), Size = new Size(f_filtros.Width, 420) };
            this.ConfigurarTablaResponsiva(tabla);
            tabla.Columns.Add("colTit", "Titular(es)");
            tabla.Columns.Add("colSac", "Sacramento");
            tabla.Columns.Add("colFec", "Fecha Oficial");
            tabla.Columns.Add("colEst", "Estado");
            tabla.Columns.Add("colDeu", "Deuda");
            this.content_frame.Controls.Add(tabla);

            Button btnExportar = CrearBotonAccion("Exportar Resumen PDF", ColorTranslator.FromHtml("#10b981"), new Point(350, 575), new Size(250, 44), 15f);
            this.content_frame.Controls.Add(btnExportar);

            Action ajustarDisenoReportes = () =>
            {
                int anchoDisp = Math.Max(780, this.content_frame.ClientSize.Width - 55);
                int altoDisp = Math.Max(260, this.content_frame.ClientSize.Height - tabla.Top - 85);

                f_filtros.Width = anchoDisp;
                tabla.Size = new Size(anchoDisp, altoDisp);

                cb_filtro_sac.Location = new Point(lblSac.Right + 8, 18);
                lblFecOf.Location = new Point(cb_filtro_sac.Right + 16, 13);
                txt_filtro_fec.Location = new Point(lblFecOf.Right + 8, 19);
                lblOrd.Location = new Point(txt_filtro_fec.Right + 16, 22);
                cb_orden.Location = new Point(lblOrd.Right + 8, 18);
                btnFiltrar.Location = new Point(cb_orden.Right + 16, 14);

                btnExportar.Location = new Point(25 + (anchoDisp - btnExportar.Width) / 2, tabla.Bottom + 15);
                f_filtros.Invalidate();
            };

            this.content_frame.Resize += (s, e) => ajustarDisenoReportes();
            ajustarDisenoReportes();

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

                for (int i = 0; i < this.db.total_registros; i++)
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
                    string texto_deuda = (deuda_val != "0" && deuda_val != "") ? ("S/ " + deuda_val) : "No tiene deuda";

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

                    tabla.Rows[rowIdx].Cells[4].Style.ForeColor = (texto_deuda == "No tiene deuda")
                        ? ColorTranslator.FromHtml("#10b981")
                        : ColorTranslator.FromHtml("#ef4444");
                }

                if (validarFiltro && tabla.Rows.Count == 0)
                {
                    MessageBox.Show("No se encontraron expedientes con los filtros actuales.", "Sin Resultados", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            };

            btnFiltrar.Click += (s, e) => actualizar_tabla(true);
            actualizar_tabla(false);

            btnExportar.Click += (s, e) =>
            {
                if (this.db.total_registros == 0)
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

                ItemReporte[] filtrados = new ItemReporte[this.db.total_registros];
                int cantFiltrados = 0;

                for (int i = 0; i < this.db.total_registros; i++)
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
                    string texto_deuda = (deuda_val != "0" && deuda_val != "") ? ("S/ " + deuda_val) : "No tiene deuda";

                    ItemReporte item = new ItemReporte();
                    item.nom = nom;
                    item.tipo = this.db.tipos[i];
                    item.dni = this.db.dnis_prin[i];
                    item.est = this.db.estados[i];
                    item.deuda = texto_deuda;
                    filtrados[cantFiltrados++] = item;
                }

                if (cantFiltrados == 0)
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
                    this.logica.generar_reporte_general_pdf(sfd.FileName, filtrados, cantFiltrados);
                    MessageBox.Show("Reporte exportado correctamente.", "Éxito", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            };
        }

        // ================= 8. OPCIONES AVANZADAS: CONSTANCIAS ANTIGUAS =================
        private void mostrar_opciones_avanzadas()
        {
            this.limpiar_contenido();

            Label lblTit = new Label() { Text = "Opciones Avanzadas - Constancias Antiguas", Font = FuentePx(24f, FontStyle.Bold), ForeColor = ColorTranslator.FromHtml("#2563eb"), Location = new Point(25, 15), AutoSize = true };
            this.content_frame.Controls.Add(lblTit);

            Label lblSub = new Label() { Text = "Registro, edición y eliminación de constancias inscritas anteriormente (Con DNI o Sin DNI)", Font = FuentePx(14f, FontStyle.Regular), ForeColor = ColorTranslator.FromHtml("#64748b"), Location = new Point(25, lblTit.Bottom + 6), AutoSize = true };
            this.content_frame.Controls.Add(lblSub);

            Panel f_form = new Panel() { Location = new Point(25, lblSub.Bottom + 15), Size = new Size(Math.Max(780, this.content_frame.ClientSize.Width - 55), 295), BackColor = Color.White, BorderStyle = BorderStyle.FixedSingle };

            int idx_hist_seleccionado = -1;
            bool cargandoExistente = false;

            Label lblSac = new Label() { Text = "Sacramento:", Font = FuentePx(13.5f, FontStyle.Bold), Location = new Point(25, 22), AutoSize = true };
            ComboBox cb_sac = new ComboBox() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 190, Font = FuentePx(13.5f) };
            cb_sac.Items.AddRange(new string[] { "Bautizo", "Primera Comunión", "Confirmación", "Matrimonio" });

            Label lblModoDni = new Label() { Text = "Tipo Registro:", Font = FuentePx(13.5f, FontStyle.Bold), AutoSize = true };
            ComboBox cb_modo_dni = new ComboBox() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 150, Font = FuentePx(13.5f) };
            cb_modo_dni.Items.AddRange(new string[] { "Con DNI", "Sin DNI" });

            Label lblDni1 = new Label() { Text = "DNI Bautizado:", Font = FuentePx(13.5f, FontStyle.Bold), AutoSize = true };
            TextBox txt_dni1 = new TextBox();
            this.adjuntar_validacion(txt_dni1, "dni");

            Label lblNom1 = new Label() { Text = "Nombre:", Font = FuentePx(13.5f, FontStyle.Bold), AutoSize = true };
            TextBox txt_nom1 = new TextBox();
            AsignarPlaceholder(txt_nom1, "NOMBRES");
            this.adjuntar_validacion(txt_nom1, "texto");

            Label lblApe1 = new Label() { Text = "Apellido:", Font = FuentePx(13.5f, FontStyle.Bold), AutoSize = true };
            TextBox txt_ape1 = new TextBox();
            AsignarPlaceholder(txt_ape1, "APELLIDOS");
            this.adjuntar_validacion(txt_ape1, "texto");

            Label lblDni2 = new Label() { Text = "DNI Novia:", Font = FuentePx(13.5f, FontStyle.Bold), AutoSize = true, Visible = false };
            TextBox txt_dni2 = new TextBox() { Visible = false };
            this.adjuntar_validacion(txt_dni2, "dni");

            Label lblNom2 = new Label() { Text = "Nombre:", Font = FuentePx(13.5f, FontStyle.Bold), AutoSize = true, Visible = false };
            TextBox txt_nom2 = new TextBox() { Visible = false };
            AsignarPlaceholder(txt_nom2, "NOMBRES NOVIA");
            this.adjuntar_validacion(txt_nom2, "texto");

            Label lblApe2 = new Label() { Text = "Apellido:", Font = FuentePx(13.5f, FontStyle.Bold), AutoSize = true, Visible = false };
            TextBox txt_ape2 = new TextBox() { Visible = false };
            AsignarPlaceholder(txt_ape2, "APELLIDOS NOVIA");
            this.adjuntar_validacion(txt_ape2, "texto");

            this.VincularAutocompletadoReniecSeparado(txt_dni1, txt_nom1, txt_ape1, txt_dni2, () => cargandoExistente);
            this.VincularAutocompletadoReniecSeparado(txt_dni2, txt_nom2, txt_ape2, txt_dni1, () => cargandoExistente);

            Label lblFec = new Label() { Text = "Fecha:", Font = FuentePx(13.5f, FontStyle.Bold), AutoSize = true };
            Label lblFecModH = new Label() { Text = "(DD/MM/AAAA)", Font = FuentePx(11.5f, FontStyle.Regular), ForeColor = ColorTranslator.FromHtml("#64748b"), AutoSize = true };
            TextBox txt_fec = new TextBox() { Width = 130 };
            this.adjuntar_validacion(txt_fec, "fecha");

            Label lblLib = new Label() { Text = "Libro:", Font = FuentePx(13.5f, FontStyle.Bold), AutoSize = true };
            TextBox txt_lib = new TextBox() { Width = 85 };
            this.adjuntar_validacion(txt_lib, "registro_libro");

            Label lblFol = new Label() { Text = "Folio:", Font = FuentePx(13.5f, FontStyle.Bold), AutoSize = true };
            TextBox txt_fol = new TextBox() { Width = 85 };
            this.adjuntar_validacion(txt_fol, "registro_libro");

            Label lblAsi = new Label() { Text = "Asiento:", Font = FuentePx(13.5f, FontStyle.Bold), AutoSize = true };
            TextBox txt_asi = new TextBox() { Width = 85 };
            this.adjuntar_validacion(txt_asi, "registro_libro");

            Button btnRegistrar = CrearBotonAccion("Registrar", ColorTranslator.FromHtml("#2563eb"), new Point(25, 222), new Size(185, 42), 14f);
            Button btnEditar = CrearBotonAccion("Editar / Actualizar", ColorTranslator.FromHtml("#10b981"), new Point(230, 222), new Size(185, 42), 14f);
            Button btnEliminar = CrearBotonAccion("Eliminar", ColorTranslator.FromHtml("#ef4444"), new Point(435, 222), new Size(185, 42), 14f);
            Button btnLimpiar = CrearBotonAccion("Limpiar Campos", ColorTranslator.FromHtml("#64748b"), new Point(640, 222), new Size(185, 42), 14f);

            f_form.Controls.AddRange(new Control[] {
                lblSac, cb_sac, lblModoDni, cb_modo_dni,
                lblDni1, txt_dni1, lblNom1, txt_nom1, lblApe1, txt_ape1,
                lblDni2, txt_dni2, lblNom2, txt_nom2, lblApe2, txt_ape2,
                lblFec, lblFecModH, txt_fec, lblLib, txt_lib, lblFol, txt_fol, lblAsi, txt_asi,
                btnRegistrar, btnEditar, btnEliminar, btnLimpiar
            });
            this.content_frame.Controls.Add(f_form);

            Action ajustarDisenoAvanzadas = () =>
            {
                int anchoDisp = Math.Max(780, this.content_frame.ClientSize.Width - 55);
                f_form.Width = anchoDisp;

                cb_sac.Location = new Point(lblSac.Right + 12, 18);
                lblModoDni.Location = new Point(cb_sac.Right + 35, 22);
                cb_modo_dni.Location = new Point(lblModoDni.Right + 12, 18);

                bool conDni = (cb_modo_dni.SelectedItem == null || cb_modo_dni.SelectedItem.ToString() == "Con DNI");
                this.DistribuirFilaPersona(f_form, 62, conDni, lblDni1, txt_dni1, lblNom1, txt_nom1, lblApe1, txt_ape1);
                this.DistribuirFilaPersona(f_form, 105, conDni, lblDni2, txt_dni2, lblNom2, txt_nom2, lblApe2, txt_ape2);

                lblFec.Location = new Point(25, 148);
                lblFecModH.Location = new Point(25, lblFec.Bottom + 1);
                int xFec = Math.Max(lblFec.Right, lblFecModH.Right) + 12;
                txt_fec.Location = new Point(xFec, 154);

                lblLib.Location = new Point(txt_fec.Right + 28, 158);
                txt_lib.Location = new Point(lblLib.Right + 8, 154);

                lblFol.Location = new Point(txt_lib.Right + 25, 158);
                txt_fol.Location = new Point(lblFol.Right + 8, 154);

                lblAsi.Location = new Point(txt_fol.Right + 25, 158);
                txt_asi.Location = new Point(lblAsi.Right + 8, 154);

                int espacioBtns = Math.Max(15, (anchoDisp - 50 - (4 * 185)) / 3);
                btnRegistrar.Location = new Point(25, 222);
                btnEditar.Location = new Point(btnRegistrar.Right + espacioBtns, 222);
                btnEliminar.Location = new Point(btnEditar.Right + espacioBtns, 222);
                btnLimpiar.Location = new Point(btnEliminar.Right + espacioBtns, 222);

                f_form.Invalidate();
            };

            this.content_frame.Resize += (s, e) => ajustarDisenoAvanzadas();

            Action buscarConstanciaExistente = () =>
            {
                if (cargandoExistente || cb_sac.SelectedItem == null || cb_modo_dni.SelectedItem == null)
                {
                    return;
                }
                string sacSel = cb_sac.SelectedItem.ToString();
                bool conDni = (cb_modo_dni.SelectedItem.ToString() == "Con DNI");
                int encontrado = -1;

                if (conDni)
                {
                    string d1 = txt_dni1.Text.Trim();
                    if (d1.Length == 8)
                    {
                        for (int i = 0; i < this.db.total_registros; i++)
                        {
                            if (this.db.origenes[i] == "ANTIGUO" && this.db.tipos[i] == sacSel && (this.db.dnis_prin[i] == d1 || this.db.dnis_sec[i] == d1))
                            {
                                encontrado = i;
                                break;
                            }
                        }
                    }
                }
                else
                {
                    string n1 = Regex.Replace(BaseDatos.A_MayusculasConTildes(txt_nom1.Text), @"\s+", " ");
                    string a1 = Regex.Replace(BaseDatos.A_MayusculasConTildes(txt_ape1.Text), @"\s+", " ");
                    if (n1.Length >= 2 && a1.Length >= 2)
                    {
                        for (int i = 0; i < this.db.total_registros; i++)
                        {
                            if (this.db.origenes[i] == "ANTIGUO" && this.db.tipos[i] == sacSel && this.db.nombres[i] == n1 && this.db.apellidos[i] == a1)
                            {
                                encontrado = i;
                                break;
                            }
                        }
                    }
                }

                if (encontrado != -1)
                {
                    cargandoExistente = true;
                    idx_hist_seleccionado = encontrado;
                    txt_nom1.Text = BaseDatos.A_MayusculasConTildes(this.db.nombres[encontrado]);
                    txt_ape1.Text = BaseDatos.A_MayusculasConTildes(this.db.apellidos[encontrado]);
                    if (sacSel == "Matrimonio")
                    {
                        txt_dni2.Text = (this.db.dnis_sec[encontrado] != "---" && this.db.dnis_sec[encontrado] != "SIN DNI") ? this.db.dnis_sec[encontrado] : "";
                        txt_nom2.Text = (this.db.nombres_sec[encontrado] != "---") ? BaseDatos.A_MayusculasConTildes(this.db.nombres_sec[encontrado]) : "";
                        txt_ape2.Text = (this.db.apellidos_sec[encontrado] != "---") ? BaseDatos.A_MayusculasConTildes(this.db.apellidos_sec[encontrado]) : "";
                    }
                    txt_fec.Text = this.db.fechas_sac[encontrado];
                    txt_lib.Text = (this.db.libros[encontrado] != "---") ? this.db.libros[encontrado] : "";
                    txt_fol.Text = (this.db.folios[encontrado] != "---") ? this.db.folios[encontrado] : "";
                    txt_asi.Text = (this.db.asientos[encontrado] != "---") ? this.db.asientos[encontrado] : "";
                    cargandoExistente = false;
                }
                else
                {
                    idx_hist_seleccionado = -1;
                }
            };

            txt_dni1.TextChanged += (s, e) => buscarConstanciaExistente();
            txt_nom1.TextChanged += (s, e) => buscarConstanciaExistente();
            txt_ape1.TextChanged += (s, e) => buscarConstanciaExistente();

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
                ajustarDisenoAvanzadas();
                buscarConstanciaExistente();
            };

            cb_sac.SelectedIndexChanged += (s, e) => actualizarVistaDniSac();
            cb_modo_dni.SelectedIndexChanged += (s, e) => actualizarVistaDniSac();

            cb_sac.SelectedItem = "Bautizo";
            cb_modo_dni.SelectedItem = "Con DNI";
            ajustarDisenoAvanzadas();

            Action limpiarCamposHist = () =>
            {
                cargandoExistente = true;
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
                cargandoExistente = false;
                actualizarVistaDniSac();
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

                string[] errores = new string[20];
                int cantErrores = 0;

                if (conDni && (d1.Length != 8 || d1 == "00000000"))
                {
                    errores[cantErrores++] = "- El " + this.ObtenerEtiquetaDniPorSacramento(sac).Replace(":", "") + " debe tener exactamente 8 dígitos numéricos válidos.";
                }
                if (!this.EsNombreValido(n1))
                {
                    errores[cantErrores++] = "- El Nombre es obligatorio (mínimo 2 letras).";
                }
                if (!this.EsNombreValido(a1))
                {
                    errores[cantErrores++] = "- El Apellido es obligatorio (mínimo 2 letras).";
                }

                if (sac == "Matrimonio")
                {
                    if (conDni)
                    {
                        if (d2.Length != 8 || d2 == "00000000")
                        {
                            errores[cantErrores++] = "- El DNI Novia debe tener 8 dígitos numéricos válidos.";
                        }
                        if (d1 != "" && d1 == d2)
                        {
                            errores[cantErrores++] = "- No puede ingresar 2 DNIs iguales. Los DNIs de los esposos deben ser diferentes.";
                        }
                    }
                    if (!this.EsNombreValido(n2) || !this.EsNombreValido(a2))
                    {
                        errores[cantErrores++] = "- El Nombre y Apellido de la Novia son obligatorios.";
                    }
                    if (n1 != "" && a1 != "" && (n1 + " " + a1) == (n2 + " " + a2))
                    {
                        errores[cantErrores++] = "- Los nombres y apellidos de ambos cónyuges no pueden ser idénticos.";
                    }
                }

                DateTime fechaObj;
                if (fec.Length != 10 || !DateTime.TryParseExact(fec, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out fechaObj))
                {
                    errores[cantErrores++] = "- La Fecha debe ser una fecha real en formato DD/MM/AAAA.";
                }
                else
                {
                    if (fechaObj.Date > DateTime.Now.Date)
                    {
                        errores[cantErrores++] = "- Al ser una Constancia Antigua, la fecha en que recibió el sacramento no puede ser futura.";
                    }
                    if (fechaObj.Year < 1900)
                    {
                        errores[cantErrores++] = "- El año de la fecha debe ser válido (mayor o igual a 1900).";
                    }
                }

                if (!this.EsNumeroPositivoValido(lib))
                {
                    errores[cantErrores++] = "- El Libro es obligatorio y solo admite números enteros positivos.";
                }
                if (!this.EsNumeroPositivoValido(fol))
                {
                    errores[cantErrores++] = "- El Folio es obligatorio y solo admite números enteros positivos.";
                }
                if (!this.EsNumeroPositivoValido(asi))
                {
                    errores[cantErrores++] = "- El Asiento es obligatorio y solo admite números enteros positivos.";
                }

                for (int i = 0; i < this.db.total_registros; i++)
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
                                errores[cantErrores++] = "- El DNI " + d1 + " ya se encuentra registrado en " + sac + ".";
                                break;
                            }
                            if (sac == "Matrimonio" && (this.db.dnis_prin[i] == d2 || this.db.dnis_sec[i] == d2))
                            {
                                errores[cantErrores++] = "- El DNI " + d2 + " ya se encuentra registrado en " + sac + ".";
                                break;
                            }
                        }
                        else
                        {
                            if (this.db.nombres[i] == n1 && this.db.apellidos[i] == a1)
                            {
                                errores[cantErrores++] = "- Ya existe un registro de " + sac + " para " + n1 + " " + a1 + ".";
                                break;
                            }
                        }
                    }
                }

                if (cantErrores > 0)
                {
                    MessageBox.Show("Por favor corrija los siguientes errores:\n\n" + this.UnirArregloConSeparador(errores, cantErrores, "\n"), "Validación de Constancia Antigua", MessageBoxButtons.OK, MessageBoxIcon.Error);
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

                string d2 = (sac == "Matrimonio") ? (conDni ? txt_dni2.Text.Trim() : "SIN DNI") : "---";
                string n2 = (sac == "Matrimonio") ? Regex.Replace(BaseDatos.A_MayusculasConTildes(txt_nom2.Text), @"\s+", " ") : "---";
                string a2 = (sac == "Matrimonio") ? Regex.Replace(BaseDatos.A_MayusculasConTildes(txt_ape2.Text), @"\s+", " ") : "---";

                this.db.registrar("ANTIGUO", d1, n1, a1, d2, n2, a2, "N/A", sac, "Finalizado", "NO", "---", "1-1-1", "Constancia histórica registrada.", "", txt_fec.Text.Trim(), txt_lib.Text.Trim(), txt_fol.Text.Trim(), txt_asi.Text.Trim(), "Pagado", "0");

                limpiarCamposHist();
                MessageBox.Show("Constancia antigua registrada correctamente en registros_sigep.csv.", "Éxito", MessageBoxButtons.OK, MessageBoxIcon.Information);
            };

            btnEditar.Click += (s, e) =>
            {
                if (idx_hist_seleccionado == -1)
                {
                    buscarConstanciaExistente();
                }
                if (idx_hist_seleccionado == -1)
                {
                    MessageBox.Show("Ingrese el DNI (o el Nombre y Apellido en modo Sin DNI) de una constancia antigua ya registrada para poder editarla.", "Registro No Encontrado", MessageBoxButtons.OK, MessageBoxIcon.Warning);
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

                string d2 = (sac == "Matrimonio") ? (conDni ? txt_dni2.Text.Trim() : "SIN DNI") : "---";
                string n2 = (sac == "Matrimonio") ? Regex.Replace(BaseDatos.A_MayusculasConTildes(txt_nom2.Text), @"\s+", " ") : "---";
                string a2 = (sac == "Matrimonio") ? Regex.Replace(BaseDatos.A_MayusculasConTildes(txt_ape2.Text), @"\s+", " ") : "---";

                this.db.actualizar_completo(idx_hist_seleccionado, d1, n1, a1, d2, n2, a2, "N/A", sac, "Finalizado", "NO", "---", "1-1-1", "Constancia histórica registrada.", "", txt_fec.Text.Trim(), txt_lib.Text.Trim(), txt_fol.Text.Trim(), txt_asi.Text.Trim(), "Pagado", "0");

                limpiarCamposHist();
                MessageBox.Show("Constancia antigua actualizada correctamente.", "Éxito", MessageBoxButtons.OK, MessageBoxIcon.Information);
            };

            btnEliminar.Click += (s, e) =>
            {
                if (idx_hist_seleccionado == -1)
                {
                    buscarConstanciaExistente();
                }
                if (idx_hist_seleccionado == -1)
                {
                    MessageBox.Show("Ingrese el DNI (o el Nombre y Apellido en modo Sin DNI) de una constancia antigua ya registrada para poder eliminarla.", "Registro No Encontrado", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                DialogResult r = MessageBox.Show("¿Está seguro de eliminar permanentemente esta constancia antigua?", "Confirmar Eliminación", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                if (r == DialogResult.Yes)
                {
                    this.db.eliminar(idx_hist_seleccionado);
                    limpiarCamposHist();
                    MessageBox.Show("Constancia antigua eliminada correctamente.", "Eliminado", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            };
        }
    }
}