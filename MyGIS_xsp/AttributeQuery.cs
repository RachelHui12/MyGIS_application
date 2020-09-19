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
using ESRI.ArcGIS.Geodatabase;
using System.Collections;

namespace MyGIS_xsp
{
    public partial class AttributeQuery : Form
    {
        public AttributeQuery(AxMapControl mapControl)
        {
            InitializeComponent();
            this.mMapControl = mapControl;

        }
        //地图数据
        private AxMapControl mMapControl;
        //选中图层
        private IFeatureLayer mFeatureLayer;


        private void AttributeQuery_Load(object sender, EventArgs e)
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
            //默认显示第一个选项
            this.cboLayer.SelectedIndex = 0;
        }

        private void cboLayer_SelectedIndexChanged(object sender, EventArgs e)
        {
            this.cboField.Items.Clear();//清空图层
            //获取cboLayer中选中的图层
            mFeatureLayer = mMapControl.get_Layer(cboLayer.SelectedIndex) as IFeatureLayer;
            label1.Text = "SELECT * FROM "+mFeatureLayer.Name+" WHERE";
            IFeatureClass pFeatureClass = mFeatureLayer.FeatureClass;
            //字段名称
            string strFldName;
            for (int i = 0; i < pFeatureClass.Fields.FieldCount; i++)
            {
                strFldName = pFeatureClass.Fields.get_Field(i).Name;
                //图层名称加入cboField
                this.cboField.Items.Add(strFldName);
            }
            //默认显示第一个选项
            this.cboField.SelectedIndex = 0;
        }

        private void cboField_SelectedIndexChanged(object sender, EventArgs e)
        {
            //获取选中的字段

        }

        private void Cancel_Click(object sender, EventArgs e)
        {
            this.Close();
            
        }

        private void cboField_DoubleClick(object sender, EventArgs e)
        {
            this.richTextBox1.Text +=" "+ this.cboField.SelectedItem+"  ";
        }

        private void OK_Click(object sender, EventArgs e)
        {
            //定义图层，要素游标，查询过滤器，要素
            IFeatureLayer pFeatureLayer;
            IFeatureCursor pFeatureCursor;
            IQueryFilter pQueryFilter;
            IFeature pFeature;
            try { 
               //获取图层
            pFeatureLayer = mFeatureLayer;
            mMapControl.Map.ClearSelection();
            //pQueryFilter的实例化
            pQueryFilter = new QueryFilterClass();
            //设置查询过滤条件
            pQueryFilter.WhereClause = this.richTextBox1.Text;
            //查询
            pFeatureCursor = pFeatureLayer.Search(pQueryFilter, true);
            //获取查询到的要素
            pFeature = pFeatureCursor.NextFeature();

            //判断是否获取到要素
            if (pFeature != null)
            {
                //选择要素
                this.mMapControl.Map.SelectFeature(pFeatureLayer, pFeature);
                //放大到要素
                this.mMapControl.Extent = pFeature.Shape.Envelope;
                this.Close();
            }
            else
            {
                //没有得到pFeature的提示
                MessageBox.Show("没有查询到数据", "提示");
            }
            }
            catch (Exception ex)
            {
                MessageBox.Show("查询语法不正确");
            }

        }

        private void tableLayoutPanel1_Click(object sender, EventArgs e)
        {
            //Button btn=this.tableLayoutPanel1.inde

        }

        private void button13_Click(object sender, EventArgs e)
        {
            listBox1.Items.Clear();
            //获取唯一值
            // 属性过滤器
            IQueryFilter pQueryFilter = new QueryFilter();
            pQueryFilter.AddField(this.cboField.SelectedItem.ToString());

            // 要素游标
            IFeatureCursor pFeatureCursor = mFeatureLayer.Search(pQueryFilter, true);
            ICursor pCursor = pFeatureCursor as ICursor;

            // 设置统计信息
            IDataStatistics pDataStatistics = new DataStatistics();
            pDataStatistics.Field = this.cboField.SelectedItem.ToString();
            pDataStatistics.Cursor = pCursor;

            // 获取唯一值
            IEnumerator uniqueValues = pDataStatistics.UniqueValues;
            uniqueValues.Reset();
            // 遍历唯一值
            while (uniqueValues.MoveNext())
            {
                listBox1.Items.Add(uniqueValues.Current.ToString());
            }
        }

        private void listBox1_DoubleClick(object sender, EventArgs e)
        {
            //if (this.listBox1.SelectedItem.GetType()==String)
            this.richTextBox1.Text += "'" + this.listBox1.SelectedItem + "'";
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Button button = (Button)sender;
            this.richTextBox1.Text += "" + button.Text + "";
        }
    }
}
