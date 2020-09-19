using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using ESRI.ArcGIS.Controls;
using ESRI.ArcGIS.Carto;

namespace MyGIS_xsp
{
    public partial class EditingLayer : Form
    {
        public EditingLayer(AxMapControl mapControl)
        {
            InitializeComponent();
            this.mMapControl = mapControl;
        }
        //地图数据
        private AxMapControl mMapControl;
        public int index = -1;
        //选中图层
        private IFeatureLayer mFeatureLayer;
        private void button1_Click(object sender, EventArgs e)
        {
            index= this.cboLayer.SelectedIndex;
            if (index != -1)
            {
                this.DialogResult = DialogResult.OK;
            }
            else { 
            
            }
        }

        private void EditingLayer_Load(object sender, EventArgs e)
        {
            //MapControl中没有图层时返回
            if (this.mMapControl.LayerCount <= 0)
                return;

            //获取MapControl中的全部图层名称，并加入ComboBox
            //图层
            ILayer pLayer;
            //图层名称
            string strLayerName;
            for (int i = 0; i < this.mMapControl.LayerCount; i++)
            {
                pLayer = this.mMapControl.get_Layer(i);
                strLayerName = pLayer.Name;
                //图层名称加入cboLayer
                this.cboLayer.Items.Add(strLayerName);
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            this.Dispose();
        }
    }
}
