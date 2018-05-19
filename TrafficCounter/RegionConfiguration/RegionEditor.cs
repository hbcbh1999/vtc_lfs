using Emgu.CV;
using Emgu.CV.Structure;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Windows.Forms;
using VTC.Common.RegionConfig;
using NLog;

namespace VTC.RegionConfiguration
{
    using Kernel.Video;
    using ThumbnailImage = Image<Bgr, float>;

    public partial class RegionEditor : Form
    {
        private Polygon _editingPolygon;
        private readonly Dictionary<Button, Polygon> _polygonLookup = new Dictionary<Button, Polygon>();

        private readonly PictureBox _preview = new PictureBox();

        private readonly IRegionConfigDataAccessLayer _regionConfigDal;

        private readonly Dictionary<string, ThumbnailImage> _thumbnails = new Dictionary<string, ThumbnailImage>();

        private BindingSource _regionConfigurationsBindingSource;

        private static readonly Logger Logger = LogManager.GetLogger("main.form");
   
        public RegionEditor(IEnumerable<ICaptureSource> captureSources, IRegionConfigDataAccessLayer regionConfigDal, RegionConfig currentlySelectedConfig = null)
        {
            InitializeComponent();

            _regionConfigDal = regionConfigDal;

            var regionConfigs = _regionConfigDal.LoadRegionConfigList();

            _regionConfigurationsBindingSource = new BindingSource { DataSource = regionConfigs };
            PopulateConfigurationList();

            lbRegionConfigurations.DataSource = _regionConfigurationsBindingSource;
            lbRegionConfigurations.DisplayMember = "Title";
            lbRegionConfigurations.ValueMember = "";

            tbRegionConfigName.DataBindings.Add("Text", _regionConfigurationsBindingSource, "Title", true, DataSourceUpdateMode.OnPropertyChanged);

            foreach (var cs in captureSources)
            {
                try
                {
                    var name = cs.Name;
                    int i = 1;
                    while (_thumbnails.Keys.Contains(name))
                    {
                        name = $"{cs.Name} ({i++})";
                    }
                    _thumbnails[name] = cs.QueryFrame().Convert<Bgr, float>();
                }
                catch (Exception e) //Todo: Make this more specific
                {
                    Logger.Log(LogLevel.Error, e.Message);
                }
                
            }
            if (!_thumbnails.Any())
            {
                _thumbnails["No capture sources found!"] = new ThumbnailImage(640, 480, new Bgr(Color.White));
            }

            var thumbnailBindingSource = new BindingSource {DataSource = _thumbnails};
            cbCaptureSource.DataSource = thumbnailBindingSource;
            cbCaptureSource.DisplayMember = "Key";
            cbCaptureSource.ValueMember = "Value";

            _preview.Dock = DockStyle.Fill;
            
            panelImage.Controls.Add(_preview);

            if (currentlySelectedConfig != null)
            {
                for (int i = 0; i < lbRegionConfigurations.Items.Count; i++)
                {
                    if (lbRegionConfigurations.Items[i].Equals(currentlySelectedConfig))
                    {
                        lbRegionConfigurations.SelectedIndex = i;
                    }
                }
            }
        }

        public RegionConfig SelectedRegionConfig => lbRegionConfigurations.SelectedItem as RegionConfig;

        private ThumbnailImage _thumbnail;
        private ThumbnailImage Thumbnail
        {
            get
            {
                return _thumbnail;
            }
            set
            {
                _thumbnail = value;
                UpdateImage();
            }
        }
        private ThumbnailImage _mask;
        private ThumbnailImage Mask
        {
            get
            {
                return _mask;
            }
            set
            {
                _mask = value;
                UpdateImage();
            }
        }

        private void UpdateImage()
        {
            if (null == Thumbnail)
            {
                _preview.Image = null;
                return;
            }

            _preview.Dock = DockStyle.None;
            _preview.Anchor = AnchorStyles.Left;
            _preview.Image = null == Mask ? Thumbnail.ToBitmap() : Thumbnail.Add(Mask).ToBitmap();
            _preview.Size = Thumbnail.ToBitmap().Size;
            panelImage.Size = _preview.Size;
        }

        private void InitializeToggleButtons(RegionConfig regionConfig)
        {
            if (null == regionConfig)
                return;

            tlpPolygonToggles.RowStyles.Clear();
            tlpPolygonToggles.RowStyles.Add(new RowStyle() { SizeType=SizeType.AutoSize });
            tlpPolygonToggles.Controls.Clear();
            tlpPolygonToggles.RowCount = 1;

            var roiButton = CreateEditRegionButton("ROI", regionConfig.RoiMask);
            AddEditAndDeleteButtons(roiButton, null, regionConfig.RoiMask);

            foreach (var regionKvp in regionConfig.Regions)
            {
                var edit = CreateEditRegionButton(regionKvp.Key, regionKvp.Value);
                var delete = CreateDeleteButton(edit, regionKvp.Value);
                AddEditAndDeleteButtons(edit, delete, regionKvp.Value);
            }
        }

        private Button CreateEditRegionButton(string text, Polygon polygon)
        {
            var button = new Button
            {
                Text = text,
                Dock = DockStyle.Fill,
                AutoSize = true,
                TextAlign = ContentAlignment.MiddleCenter
            };


            button.MouseEnter += tb_MouseEnter;
            button.MouseLeave += tb_MouseLeave;
            button.Click += (sender, args) =>
            {
                SetEditing(true, button, polygon);
            };

            return button;
        }

        private Button CreateDeleteButton(Button editButton, Polygon polygon)
        {
            var deleteButton = new Button
            {
                Font =
                    new Font("Microsoft Sans Serif", 8.25F, FontStyle.Bold,
                        GraphicsUnit.Point, 0),
                Size = new Size(22, 22),
                Text = "X",
                UseVisualStyleBackColor = false
            };


            deleteButton.Click += (sender, args) =>
            {
                if (DialogResult.Yes == MessageBox.Show("Remove region " + editButton.Text + "?", string.Empty, MessageBoxButtons.YesNo))
                {
                    foreach(var kvp in SelectedRegionConfig.Regions.Where(r => r.Value == polygon).ToList())
                    {
                        SelectedRegionConfig.Regions.Remove(kvp.Key);
                    }
                    
                    tlpPolygonToggles.Controls.Remove(deleteButton);
                    tlpPolygonToggles.Controls.Remove(editButton);
                    _polygonLookup.Remove(editButton);
                }
            };

            return deleteButton;
        }



        private void AddEditAndDeleteButtons(Button edit, Button delete, Polygon polygon)
        {
            if (null != polygon)
                _polygonLookup[edit] = polygon;

            tlpPolygonToggles.Controls.Remove(btnAddApproachExit);
            if (null != edit)
                tlpPolygonToggles.Controls.Add(edit, 0, tlpPolygonToggles.RowCount - 1);
            if (null != delete) 
                tlpPolygonToggles.Controls.Add(delete, 1, tlpPolygonToggles.RowCount - 1);

            tlpPolygonToggles.RowCount++;
            tlpPolygonToggles.Controls.Add(btnAddApproachExit, 0, tlpPolygonToggles.RowCount - 1);
        }

        void tb_MouseLeave(object sender, EventArgs e)
        {
            Mask = null;
        }

        void tb_MouseEnter(object sender, EventArgs e)
        {
            var rb = sender as Button;
            if (rb == null) return;
            var polygon = _polygonLookup[rb];
            var mask = polygon.GetMask(Thumbnail.Width, Thumbnail.Height, new Bgr(Color.Blue));
            Mask = mask;
        }

        private void SetEditing(bool editing, Button activeButton, Polygon polygon)
        {
            // Disable all buttons while editing
            tlpRegionConfigSelector.Enabled = !editing;
            btnOK.Enabled = !editing;
            btnCancel.Enabled = !editing;
            foreach (var control in tlpRegionConfigEditor.Controls)
            {
                if (control == panelImage || !(control is Control))
                    continue;

                ((Control)control).Enabled = !editing;
            }

            if (editing)
            {
                _editingPolygon = polygon;

                if (null != activeButton)
                {
                    var control = new PolygonBuilderControl(Thumbnail, _polygonLookup[activeButton])
                    {
                        Dock = DockStyle.Fill
                    };
                    control.OnDoneClicked += (sender, args) =>
                    {
                        _editingPolygon.Clear();
                        foreach (var coord in control.Coordinates)
                        {
                            _editingPolygon.Add(coord);
                        }
                        polygon.UpdateCentroid();
                        SetEditing(false, null, null);
                    };
                    control.OnCancelClicked += (sender, args) =>
                    {
                        SetEditing(false, null, null);
                    };
                    panelImage.Controls.Clear();
                    panelImage.Controls.Add(control);
                }
            }
            else
            {
                // Restore the preview image
                panelImage.Controls.Clear();
                panelImage.Controls.Add(_preview);
            }
        }

        private void btnAddApproachExit_Click(object sender, EventArgs e)
        {
            var selectedRegionConfig = lbRegionConfigurations.SelectedItem as RegionConfig;
            if (null == selectedRegionConfig)
                return;

            var input = new InputPrompt("Region Name", "Enter Region Name:");
            if (DialogResult.OK != input.ShowDialog())
                return;

            var polygon = new Polygon();
            var edit = CreateEditRegionButton(input.InputString, polygon);
            var delete = CreateDeleteButton(edit, polygon);
            AddEditAndDeleteButtons(edit, delete, polygon);

            selectedRegionConfig.Regions.Add(input.InputString, polygon);
        }

        private object _previousSelectedValue;
        private void lbRegionConfigurations_SelectedValueChanged(object sender, EventArgs e)
        {
            if (_previousSelectedValue == lbRegionConfigurations.SelectedValue)
                return;

            if (null == lbRegionConfigurations.SelectedValue)
                return;

            _previousSelectedValue = lbRegionConfigurations.SelectedValue;

            var selectedRegionConfig = lbRegionConfigurations.SelectedItem as RegionConfig;
            if (null == selectedRegionConfig)
                return;

            InitializeToggleButtons(selectedRegionConfig);
        }

        private void PopulateConfigurationList()
        {
            int currentHeight = 20;
            int x_offset = 20;
            panel1.Controls.Clear();

            foreach (var p in typeof(RegionConfig).GetProperties())
            {
                if (p.PropertyType.Name == "Boolean")
                {
                    var newCheckbox = new CheckBox();
                    newCheckbox.SetBounds(x_offset, currentHeight, 250, 20);
                    newCheckbox.Text = p.Name;
                    currentHeight += newCheckbox.Height + 5;
                    newCheckbox.Name = p.Name + "checkBox";
                    panel1.Controls.Add(newCheckbox);
                    newCheckbox.DataBindings.Add("Checked", _regionConfigurationsBindingSource, p.Name, true, DataSourceUpdateMode.OnPropertyChanged);
                }
                else if (p.PropertyType.Name == "String" || p.PropertyType.Name == "Int32" || p.PropertyType.Name == "Double")
                {
                    var newControl = new TextBox();
                    newControl.SetBounds(x_offset, currentHeight, 200, 20);
                    var newLabel = new Label();
                    newLabel.SetBounds(x_offset + newControl.Width + 5, currentHeight, 250, 20);
                    newLabel.Text = p.Name;
                    currentHeight += newControl.Height + 5;
                    newControl.Name = p.Name + "textBox";
                    panel1.Controls.Add(newControl);
                    panel1.Controls.Add(newLabel);
                    newControl.DataBindings.Add("Text", _regionConfigurationsBindingSource, p.Name, true, DataSourceUpdateMode.OnPropertyChanged);
                }
            }
        }

        private void btnAdd_Click(object sender, EventArgs e)
        {
            var input = new InputPrompt("Region Configuration Name", "Enter Region Configuration Name:");
            if (DialogResult.OK != input.ShowDialog())
                return;

            var newConfig = new RegionConfig()
            {
                Title = input.InputString
            };
            _regionConfigurationsBindingSource.Add(newConfig);
            lbRegionConfigurations.SelectedItem = newConfig;
        }

        private void btnDelete_Click(object sender, EventArgs e)
        {
            var item = lbRegionConfigurations.SelectedItem as RegionConfig;
            if (null == item)
                return;

            var result = MessageBox.Show("Are you sure you want to delete " + item.Title, "Confirm Delete", MessageBoxButtons.YesNo);

            if (result == DialogResult.Yes)
            {
                _regionConfigurationsBindingSource.Remove(item);
            }
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            var result = MessageBox.Show("Discard changes?", "Confirm", MessageBoxButtons.YesNo);

            if (result != DialogResult.Yes)
                return;

            DialogResult = DialogResult.Cancel;

            Close();
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.OK;

            var regionConfigs = _regionConfigurationsBindingSource.DataSource as List<RegionConfig>;
            if (null != regionConfigs)
            {
                _regionConfigDal.SaveRegionConfigList(regionConfigs);
            }

            Close();
        }

        private void cbCaptureSource_SelectedValueChanged(object sender, EventArgs e)
        {
            Thumbnail = cbCaptureSource.SelectedValue as Image<Bgr, float>;
        }

        private void importButton_Click(object sender, EventArgs e)
        {
            //Select file
            if (importRegionConfigFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                //Deserialize RegionConfig
                System.IO.StreamReader sr = new System.IO.StreamReader(importRegionConfigFileDialog.FileName);
                System.Xml.XmlReader xr = new System.Xml.XmlTextReader(sr);
                
                DataContractSerializer s = new DataContractSerializer(typeof(RegionConfig));
                var importedRegionConfig = (RegionConfig) s.ReadObject(xr);
                xr.Close();
                sr.Close();

                //Add to list of RegionConfigs
                var regionConfigs = _regionConfigDal.LoadRegionConfigList();
                regionConfigs.Add(importedRegionConfig);
                _regionConfigurationsBindingSource = new BindingSource { DataSource = regionConfigs };

                lbRegionConfigurations.DataSource = _regionConfigurationsBindingSource;
                lbRegionConfigurations.DisplayMember = "Title";
                lbRegionConfigurations.ValueMember = "";
                tbRegionConfigName.DataBindings.Clear();
                tbRegionConfigName.DataBindings.Add("Text", _regionConfigurationsBindingSource, "Title", true, DataSourceUpdateMode.OnPropertyChanged);

                lbRegionConfigurations.SelectedIndex = lbRegionConfigurations.Items.Count - 1;
                lbRegionConfigurations_SelectedValueChanged(null, null);

                PopulateConfigurationList();

                btnOK_Click(null, null);
            }
        }

        private void exportButton_Click(object sender, EventArgs e)
        {
            //Choose file location
            if (exportRegionConfigFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                //Serialize RegionConfig
                System.IO.StreamWriter sr = new System.IO.StreamWriter(exportRegionConfigFileDialog.FileName);
                System.Xml.XmlWriter xr = new System.Xml.XmlTextWriter(sr);
                DataContractSerializer s = new DataContractSerializer(typeof(RegionConfig));
                s.WriteObject(xr, SelectedRegionConfig);
                xr.Close();
                sr.Close();
            }
        }
    }
}
