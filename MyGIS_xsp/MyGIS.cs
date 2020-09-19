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
using ESRI.ArcGIS.Geometry;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Display;
using ESRI.ArcGIS.NetworkAnalysis;
using ESRI.ArcGIS.DataSourcesGDB;
using ESRI.ArcGIS.Analyst3D;
namespace MyGIS_xsp
{
    public partial class MyGIS : Form
    {
        public MyGIS()
        {
            InitializeComponent();
            //mTINType.SelectedIndex = 0;
        }
        #region 成员变量
        ZoomIn mZoomIn = null;
        ZoomOut mZoomOut = null;
        FixedZoomIn fixedZoomin = null;
        FixedZoomOut fixedZoomout = null;
        Pan pan = null;
        private string mTool;//标记当前选中的工具类型
        int flag = 0;
        //空间查询的查询方式
        private int mQueryMode;
        //图层索引
        private int mLayerIndex;
        //几何网络
        private IGeometricNetwork mGeometricNetwork;
        //给定点的集合
        private IPointCollection mPointCollection;
        //获取给定点最近的Network元素
        private IPointToEID mPointToEID;

        //返回结果变量
        private IEnumNetEID mEnumNetEID_Junctions;
        private IEnumNetEID mEnumNetEID_Edges;
        private double mdblPathCost;
        private Edit mEdit;
        //点查询接口
        private IHit3DSet mHit3DSet;
        private ResultForm mResultForm = new ResultForm();
        #endregion
        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //文件路径名称,包含文件名称和路径名称
            string strName = null;

            //定义OpenFileDialog，获取并打开地图文档
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Title = "打开MXD";
            openFileDialog.Filter = "MXD文件（*.mxd）|*.mxd";
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                strName = openFileDialog.FileName;
                if (strName != "")
                {
                    this.axMapControl1.LoadMxFile(strName);
                }
            }
            //地图文档全图显示
            this.axMapControl1.Extent = this.axMapControl1.FullExtent;

        }


        private void addShpToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //文件路径名称，包含文件名称和路径名称
            string strName = null;
            //文件路径
            string strFilePath = null;
            //文件名称
            string strFileName = null;

            //定义OpenFileDialog，获取并打开地图文档
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Title = "添加Shp";
            openFileDialog.Filter = "shp文件（*.shp）|*.shp";
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                strName = openFileDialog.FileName;
                if (strName != "")
                {
                    strFilePath = System.IO.Path.GetDirectoryName(strName);
                    strFileName = System.IO.Path.GetFileNameWithoutExtension(strName);
                    this.axMapControl1.AddShapeFile(strFilePath, strFileName);
                }
            }
            //地图文档全图显示
            this.axMapControl1.Extent = this.axMapControl1.FullExtent;

        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //文件路径名称,包含文件名称和路径名称
            string strName = null;

            //定义OpenFileDialog，获取并打开地图文档
            SaveFileDialog saveDialog = new SaveFileDialog();
            saveDialog.Title = "保存";
            saveDialog.Filter = "MXD文件（*.mxd）|*.mxd";
            //保存时覆盖同名文件
            saveDialog.OverwritePrompt = true;
            //对话框关闭前还原当前目录
            saveDialog.RestoreDirectory = true;
            if (saveDialog.ShowDialog() == DialogResult.OK)
            {
                saveDialog.CheckPathExists = true;
                IMxdContents pMxdc;
                pMxdc = this.axMapControl1.Map as IMxdContents;
                IMapDocument pmap = new MapDocument();
                pmap.Open(axMapControl1.DocumentFilename,"");
                IActiveView pactivreview = axMapControl1.Map as IActiveView;
                pmap.ReplaceContents(pMxdc);
                pmap.SaveAs(saveDialog.FileName, true, true);
                //this.axMapControl1.sa
            }

        }

        private void zoomInToolStripMenuItem_Click(object sender, EventArgs e)
        {
            mZoomIn = new ZoomIn();
            //与MapControl的关联
            mZoomIn.OnCreate(this.axMapControl1.Object);
            //设置鼠标形状
            this.axMapControl1.MousePointer = esriControlsMousePointer.esriPointerZoomIn;
            flag = 1;
            this.mTool = "ZoomIn";
        }
        #region 鼠标响应操作
        private void axMapControl1_OnMouseDown(object sender, IMapControlEvents2_OnMouseDownEvent e)
        {
            this.axMapControl1.Map.ClearSelection();
            //获取当前视图
            IActiveView pActiveView = this.axMapControl1.ActiveView;
            //获取鼠标点
            IPoint pPoint = pActiveView.ScreenDisplay.DisplayTransformation.ToMapPoint(e.x, e.y);
            switch (mTool)
            {
                case"ZoomIn":
                    mZoomIn.OnMouseDown(e.button, e.shift, e.x, e.y);
                    break;
                case "ZoomOut":
                    mZoomOut.OnMouseDown(e.button, e.shift, e.x, e.y);
                    break;
                case "Pan":
                    pan.OnMouseDown(e.button, e.shift, e.x, e.y);
                    break;
                case "SpaceQuery":
                    IGeometry pGeometry = null;
                    if (this.mQueryMode == 0)//矩形查询
                    {
                        pGeometry = this.axMapControl1.TrackRectangle();
                    }
                    else if (this.mQueryMode == 1)//线查询
                    {
                        pGeometry = this.axMapControl1.TrackLine();
                    }
                    else if (this.mQueryMode == 2)//点查询
                    {
                        ITopologicalOperator pTopo;
                        IGeometry pBuffer;
                        pGeometry = pPoint;
                        pTopo = pGeometry as ITopologicalOperator;
                        //根据点位创建缓冲区，缓冲半径为0.1，可修改
                        pBuffer = pTopo.Buffer(0.1);
                        pGeometry = pBuffer.Envelope;
                    }
                    else if (this.mQueryMode == 3)//圆查询
                    {
                        pGeometry = this.axMapControl1.TrackCircle();
                    }
                    IFeatureLayer pFeatureLayer = this.axMapControl1.get_Layer(this.mLayerIndex) as IFeatureLayer;
                    DataTable pDataTable = this.LoadQueryResult(this.axMapControl1, pFeatureLayer, pGeometry);
                    this.dataGridView1.DataSource = pDataTable.DefaultView;

                    this.dataGridView1.Refresh();
                    break;
                case "Network":
                    //记录鼠标点击的点
                    IPoint pNewPoint = new PointClass();
                    pNewPoint.PutCoords(e.mapX, e.mapY);

                    if (mPointCollection == null)
                        mPointCollection = new MultipointClass();
                    //添加点，before和after标记添加点的索引，这里不定义
                    object before = Type.Missing;
                    object after = Type.Missing;
                    mPointCollection.AddPoint(pNewPoint, ref before, ref after);
                    break;
                default:
                    break;

            }
            if (e.button != 1)
                return;
            //判断是否处于编辑状态
            if (mEdit!=null && mEdit.IsEditing())
            {
                switch (cboTasks.SelectedIndex)
                {
                    case 0:
                        mEdit.CreateMouseDown(e.mapX, e.mapY);
                        break;
                    case 1:
                        mEdit.PanMouseDown(e.mapX, e.mapY);
                        break;
                }
            }
        }

        private void axMapControl1_OnMouseMove(object sender, IMapControlEvents2_OnMouseMoveEvent e)
        {
            switch (mTool)
            {
                case "ZoomIn":
                    mZoomIn.OnMouseMove(e.button, e.shift, e.x, e.y);
                    break;
                case "ZoomOut":
                    mZoomOut.OnMouseMove(e.button, e.shift, e.x, e.y);
                    break;
                case "Pan":
                    pan.OnMouseMove(e.button, e.shift, e.x, e.y);
                    break;
                default:
                    break;

            }
            // 显示当前比例尺
            this.statusScale.Text = " 比例尺 1:" + ((long)this.axMapControl1.MapScale).ToString();
            // 显示当前坐标
            this.statueCoordinate.Text = " 当前坐标 X = " + e.mapX.ToString() + " Y = " + e.mapY.ToString() + " " + this.axMapControl1.MapUnits;
            //判断是否处于编辑状态
            if (mEdit != null && mEdit.IsEditing())
            {
                switch (cboTasks.SelectedIndex)
                {
                    case 0:
                    case 1:
                        mEdit.MouseMove(e.mapX, e.mapY);
                        break;
                }
            }
        }

        private void axMapControl1_OnMouseUp(object sender, IMapControlEvents2_OnMouseUpEvent e)
        {
            switch (mTool)
            {
                case "ZoomIn":
                    mZoomIn.OnMouseUp(e.button, e.shift, e.x, e.y);
                    break;
                case "ZoomOut":
                    mZoomOut.OnMouseUp(e.button, e.shift, e.x, e.y);
                    break;
                case "Pan":
                    pan.OnMouseUp(e.button, e.shift, e.x, e.y);
                    break;
                default:
                    break;

            }
            //判断是否鼠标左键
            if (e.button != 1)
                return;
            //判断是否处于编辑状态
            if (mEdit != null && mEdit.IsEditing())
            {
                switch (cboTasks.SelectedIndex)
                {
                    case 0:
                        break;
                    case 1:
                        mEdit.PanMouseUp(e.mapX, e.mapY);
                        break;
                }
            }
        }

        private void axMapControl1_OnDoubleClick(object sender, IMapControlEvents2_OnDoubleClickEvent e)
        {
            switch (mTool)
            { 
                case "Network":
                    try
                    {
                        //路径计算
                        //注意权重名称与设置保持一致
                        SolvePath("LENGTH");
                        //路径转换为几何要素
                        IPolyline pPolyLineResult = PathToPolyLine();
                        //获取屏幕显示
                        IActiveView pActiveView = this.axMapControl1.ActiveView;
                        IScreenDisplay pScreenDisplay = pActiveView.ScreenDisplay;
                        //设置显示符号
                        ILineSymbol pLineSymbol = new CartographicLineSymbolClass();
                        IRgbColor pColor = new RgbColorClass();
                        pColor.Red = 255;
                        pColor.Green = 0;
                        pColor.Blue = 0;
                        //设置线宽
                        pLineSymbol.Width = 4;
                        //设置颜色
                        pLineSymbol.Color = pColor as IColor;
                        //绘制线型符号
                        pScreenDisplay.StartDrawing(0, 0);
                        pScreenDisplay.SetSymbol((ISymbol)pLineSymbol);
                        pScreenDisplay.DrawPolyline(pPolyLineResult);
                        pScreenDisplay.FinishDrawing();
                    }
                    catch (System.Exception ex)
                    {
                        MessageBox.Show("路径分析出现错误:" + "\r\n" + ex.Message);
                    }
                    //点集设为空
                    mPointCollection = null;
                    break;
                case "Edit":
                    //判断是否鼠标左键
                    if (e.button != 1)
                        return;

                    ////判断是否处于编辑状态
                    if (mEdit != null && mEdit.IsEditing())
                    {
                        switch (cboTasks.SelectedIndex)
                        {
                            case 0:
                                mEdit.CreateDoubleClick(e.mapX, e.mapY);
                                break;
                            case 1:
                                break;
                        }
                    }
                    break;
                default:
                    break;
            }
            

           
        }
        #endregion

        private void fixedZoomInToolStripMenuItem_Click(object sender, EventArgs e)
        {
            fixedZoomin = new FixedZoomIn();
            //与MapControl关联
            fixedZoomin.OnCreate(this.axMapControl1.Object);
            fixedZoomin.OnClick();
            flag = 3;
            this.mTool = "fixedZoomIn";

        }

        private void zoomOutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            mZoomOut = new ZoomOut();
            //与MapControl的关联
            mZoomOut.OnCreate(this.axMapControl1.Object);
            //设置鼠标形状
            this.axMapControl1.MousePointer = esriControlsMousePointer.esriPointerZoomOut;
            this.mTool = "ZoomOut";

        }

        private void fixedZoomOutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            fixedZoomout = new FixedZoomOut();
            //与MapControl关联
            fixedZoomout.OnCreate(this.axMapControl1.Object);
            fixedZoomout.OnClick();
            flag = 4;
            this.mTool = "fixedZoomOut";

        }

        private void menuPanToolStripMenuItem_Click(object sender, EventArgs e)
        {
            pan = new Pan();
            //与MapControl关联
            pan.OnCreate(this.axMapControl1.Object);
            this.axMapControl1.MousePointer = esriControlsMousePointer.esriPointerPan;
            this.mTool = "Pan";

        }

        private void fullExtentToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.axMapControl1.Extent = this.axMapControl1.FullExtent;
        }

        private void attributeQueryToolStripMenuItem_Click(object sender, EventArgs e)
        {
            AttributeQuery form1 = new AttributeQuery(this.axMapControl1);
            form1.Show();
        }

        private void spatialQueryToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SpatialQuery spatialQueryForm = new SpatialQuery(this.axMapControl1);
            if (spatialQueryForm.ShowDialog() == DialogResult.OK)
            {
                //标记为“空间查询”
                this.mTool = "SpaceQuery";
                //获取查询方式和图层
                this.mQueryMode = spatialQueryForm.mQueryMode;
                this.mLayerIndex = spatialQueryForm.mLayerIndex;
                //定义鼠标形状
                this.axMapControl1.MousePointer = ESRI.ArcGIS.Controls.esriControlsMousePointer.esriPointerCrosshair;
            }

        }
        private DataTable LoadQueryResult(AxMapControl mapControl, IFeatureLayer featureLayer, IGeometry geometry)
        {
            IFeatureClass pFeatureClass = featureLayer.FeatureClass;

            //根据图层属性字段初始化DataTable
            IFields pFields = pFeatureClass.Fields;
            DataTable pDataTable = new DataTable();
            for (int i = 0; i < pFields.FieldCount; i++)
            {
                string strFldName;
                strFldName = pFields.get_Field(i).AliasName;
                pDataTable.Columns.Add(strFldName);
            }

            //空间过滤器
            ISpatialFilter pSpatialFilter = new SpatialFilterClass();
            pSpatialFilter.Geometry = geometry;

            //根据图层类型选择缓冲方式
            switch (pFeatureClass.ShapeType)
            {
                case esriGeometryType.esriGeometryPoint:
                    pSpatialFilter.SpatialRel = esriSpatialRelEnum.esriSpatialRelContains;
                    break;
                case esriGeometryType.esriGeometryPolyline:
                    pSpatialFilter.SpatialRel = esriSpatialRelEnum.esriSpatialRelCrosses;
                    break;
                case esriGeometryType.esriGeometryPolygon:
                    pSpatialFilter.SpatialRel = esriSpatialRelEnum.esriSpatialRelIntersects;
                    break;
            }
            //定义空间过滤器的空间字段
            pSpatialFilter.GeometryField = pFeatureClass.ShapeFieldName;

            IQueryFilter pQueryFilter;
            IFeatureCursor pFeatureCursor;
            IFeature pFeature;
            //利用要素过滤器查询要素
            pQueryFilter = pSpatialFilter as IQueryFilter;
            pFeatureCursor = featureLayer.Search(pQueryFilter, true);
            pFeature = pFeatureCursor.NextFeature();

            while (pFeature != null)
            {
                string strFldValue = null;
                DataRow dr = pDataTable.NewRow();
                //遍历图层属性表字段值，并加入pDataTable
                for (int i = 0; i < pFields.FieldCount; i++)
                {
                    string strFldName = pFields.get_Field(i).Name;
                    if (strFldName == "Shape")
                    {
                        strFldValue = Convert.ToString(pFeature.Shape.GeometryType);
                    }
                    else
                        strFldValue = Convert.ToString(pFeature.get_Value(i));
                    dr[i] = strFldValue;
                }
                pDataTable.Rows.Add(dr);
                //高亮选择要素
                mapControl.Map.SelectFeature((ILayer)featureLayer, pFeature);
                mapControl.ActiveView.Refresh();
                pFeature = pFeatureCursor.NextFeature();
            }
            return pDataTable;
        }

        private void axMapControl1_OnExtentUpdated(object sender, IMapControlEvents2_OnExtentUpdatedEvent e)
        {
            //创建鹰眼中线框
            IEnvelope pEnv = (IEnvelope)e.newEnvelope;
            IRectangleElement pRectangleEle = new RectangleElementClass();
            IElement pEle = pRectangleEle as IElement;
            pEle.Geometry = pEnv;

            //设置线框的边线对象，包括颜色和线宽
            IRgbColor pColor = new RgbColorClass();
            pColor.Red = 255;
            pColor.Green = 0;
            pColor.Blue = 0;
            pColor.Transparency = 255;
            // 产生一个线符号对象 
            ILineSymbol pOutline = new SimpleLineSymbolClass();
            pOutline.Width = 2;
            pOutline.Color = pColor;

            // 设置颜色属性 
            pColor.Red = 255;
            pColor.Green = 0;
            pColor.Blue = 0;
            pColor.Transparency = 0;

            // 设置线框填充符号的属性 
            IFillSymbol pFillSymbol = new SimpleFillSymbolClass();
            pFillSymbol.Color = pColor;
            pFillSymbol.Outline = pOutline;
            IFillShapeElement pFillShapeEle = pEle as IFillShapeElement;
            pFillShapeEle.Symbol = pFillSymbol;

            // 得到鹰眼视图中的图形元素容器
            IGraphicsContainer pGra = axMapControl2.Map as IGraphicsContainer;
            IActiveView pAv = pGra as IActiveView;
            // 在绘制前，清除 axMapControl2 中的任何图形元素 
            pGra.DeleteAllElements();
            // 鹰眼视图中添加线框
            pGra.AddElement((IElement)pFillShapeEle, 0);
            // 刷新鹰眼
            pAv.PartialRefresh(esriViewDrawPhase.esriViewGraphics, null, null);    

        }

        private void axMapControl2_OnMouseDown(object sender, IMapControlEvents2_OnMouseDownEvent e)
        {
            if (this.axMapControl2.Map.LayerCount != 0)
            {
                // 按下鼠标左键移动矩形框 
                if (e.button == 1)
                {
                    IPoint pPoint = new PointClass();
                    pPoint.PutCoords(e.mapX, e.mapY);
                    IEnvelope pEnvelope = this.axMapControl1.Extent;
                    pEnvelope.CenterAt(pPoint);
                    this.axMapControl1.Extent = pEnvelope;
                    this.axMapControl1.ActiveView.PartialRefresh(esriViewDrawPhase.esriViewGeography, null, null);
                }
                // 按下鼠标右键绘制矩形框 
                else if (e.button == 2)
                {
                    IEnvelope pEnvelop = this.axMapControl2.TrackRectangle();
                    this.axMapControl1.Extent = pEnvelop;
                    this.axMapControl1.ActiveView.PartialRefresh(esriViewDrawPhase.esriViewGeography, null, null);
                }
            }

        }

        private void axMapControl2_OnMouseMove(object sender, IMapControlEvents2_OnMouseMoveEvent e)
        {
            // 如果不是左键按下就直接返回 
            if (e.button != 1) return;
            IPoint pPoint = new PointClass();
            pPoint.PutCoords(e.mapX, e.mapY);
            this.axMapControl1.CenterAt(pPoint);
            this.axMapControl1.ActiveView.PartialRefresh(esriViewDrawPhase.esriViewGeography, null, null);

        }

        private ILayer GetOverviewLayer(IMap map)
        {
            if (map.LayerCount == 0)
                return null;
            //获取主视图的第一个图层
            ILayer pLayer = map.get_Layer(0);
            //遍历其他图层，并比较视图范围的宽度，返回宽度最大的图层
            ILayer pTempLayer = null;
            for (int i = 1; i < map.LayerCount; i++)
            {
                pTempLayer = map.get_Layer(i);
                if (pLayer.AreaOfInterest.Width < pTempLayer.AreaOfInterest.Width)
                    pLayer = pTempLayer;
            }
            return pLayer;
        }

        private void axMapControl2_OnMapReplaced(object sender, IMapControlEvents2_OnMapReplacedEvent e)
        {
            if (this.GetOverviewLayer(this.axMapControl1.Map) == null)
                return;
            //获取鹰眼图层
            this.axMapControl2.AddLayer(this.GetOverviewLayer(this.axMapControl1.Map));
            // 设置 MapControl 显示范围至数据的全局范围
            this.axMapControl2.Extent = this.axMapControl1.FullExtent;
            // 刷新鹰眼控件地图
            this.axMapControl2.Refresh();

        }

        private void axMapControl1_OnFullExtentUpdated(object sender, IMapControlEvents2_OnFullExtentUpdatedEvent e)
        {

            this.axMapControl2.ClearLayers();
            for (int i = this.axMapControl1.LayerCount-1; i >=0; i--)
            {
                this.axMapControl2.AddLayer(this.axMapControl1.get_Layer(i));
            }
            this.axMapControl2.Extent = this.axMapControl1.FullExtent;
            //this.axMapControl2.Refresh();
        }

        private void bufferToolStripMenuItem_Click(object sender, EventArgs e)
        {
            BufferForm bufferForm = new BufferForm(this.axMapControl1);
            if (bufferForm.ShowDialog() == DialogResult.OK)
            {
                //获取输出文件路径
                string strBufferPath = bufferForm.strOutputPath;
                //缓冲区图层载入到MapControl
                int index = strBufferPath.LastIndexOf("\\");
                this.axMapControl1.AddShapeFile(strBufferPath.Substring(0, index), strBufferPath.Substring(index));
            }

        }

        private void overLayToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OverlayForm overlayform = new OverlayForm();
            if (overlayform.ShowDialog() == DialogResult.OK)
            {
                string strOverlayPath = overlayform.strOutputPath;
                int index = strOverlayPath.LastIndexOf("\\");
                this.axMapControl1.AddShapeFile(strOverlayPath.Substring(0, index), strOverlayPath.Substring(index));
            }
        }


        #region 网络分析自定义函数

        /// <summary>
        /// 路径计算
        /// </summary>
        /// <param name="weightName">权重名称</param>
        private void SolvePath(string weightName)
        {
            //创建ITraceFlowSolverGEN
            ITraceFlowSolverGEN pTraceFlowSolverGEN = new TraceFlowSolverClass();
            INetSolver pNetSolver = pTraceFlowSolverGEN as INetSolver;
            //初始化用于路径计算的Network
            INetwork pNetWork = mGeometricNetwork.Network;
            pNetSolver.SourceNetwork = pNetWork;

            //获取分析经过的点的个数
            int intCount = mPointCollection.PointCount;
            if (intCount < 1)
                return;


            INetFlag pNetFlag;
            //用于存储路径计算得到的边
            IEdgeFlag[] pEdgeFlags = new IEdgeFlag[intCount];


            IPoint pEdgePoint = new PointClass();
            int intEdgeEID;
            IPoint pFoundEdgePoint;
            double dblEdgePercent;

            //用于获取几何网络元素的UserID, UserClassID,UserSubID
            INetElements pNetElements = pNetWork as INetElements;
            int intEdgeUserClassID;
            int intEdgeUserID;
            int intEdgeUserSubID;
            for (int i = 0; i < intCount; i++)
            {
                pNetFlag = new EdgeFlagClass();
                //获取用户点击点
                pEdgePoint = mPointCollection.get_Point(i);
                //获取距离用户点击点最近的边
                mPointToEID.GetNearestEdge(pEdgePoint, out intEdgeEID, out pFoundEdgePoint, out dblEdgePercent);
                if (intEdgeEID <= 0)
                    continue;
                //根据得到的边查询对应的几何网络中的元素UserID, UserClassID,UserSubID
                pNetElements.QueryIDs(intEdgeEID, esriElementType.esriETEdge,
                    out intEdgeUserClassID, out intEdgeUserID, out intEdgeUserSubID);
                if (intEdgeUserClassID <= 0 || intEdgeUserID <= 0)
                    continue;

                pNetFlag.UserClassID = intEdgeUserClassID;
                pNetFlag.UserID = intEdgeUserID;
                pNetFlag.UserSubID = intEdgeUserSubID;
                pEdgeFlags[i] = pNetFlag as IEdgeFlag;
            }
            //设置路径求解的边
            pTraceFlowSolverGEN.PutEdgeOrigins(ref pEdgeFlags);

            //路径计算权重
            INetSchema pNetSchema = pNetWork as INetSchema;
            INetWeight pNetWeight = pNetSchema.get_WeightByName(weightName);
            if (pNetWeight == null)
                return;

            //设置权重，这里双向的权重设为一致
            INetSolverWeights pNetSolverWeights = pTraceFlowSolverGEN as INetSolverWeights;
            pNetSolverWeights.ToFromEdgeWeight = pNetWeight;
            pNetSolverWeights.FromToEdgeWeight = pNetWeight;

            object[] arrResults = new object[intCount - 1];
            //执行路径计算
            pTraceFlowSolverGEN.FindPath(esriFlowMethod.esriFMConnected, esriShortestPathObjFn.esriSPObjFnMinSum,
                out mEnumNetEID_Junctions, out mEnumNetEID_Edges, intCount - 1, ref arrResults);

            //获取路径计算总代价（cost）
            mdblPathCost = 0;
            for (int i = 0; i < intCount - 1; i++)
                mdblPathCost += (double)arrResults[i];
        }

        /// <summary>
        /// 路径转换为几何要素
        /// </summary>
        /// <returns></returns>
        private IPolyline PathToPolyLine()
        {
            IPolyline pPolyLine = new PolylineClass();
            IGeometryCollection pNewGeometryCollection = pPolyLine as IGeometryCollection;
            if (mEnumNetEID_Edges == null)
                return null;

            IEIDHelper pEIDHelper = new EIDHelperClass();
            //获取几何网络
            pEIDHelper.GeometricNetwork = mGeometricNetwork;
            //获取地图空间参考
            ISpatialReference pSpatialReference = this.axMapControl1.Map.SpatialReference;
            pEIDHelper.OutputSpatialReference = pSpatialReference;
            pEIDHelper.ReturnGeometries = true;
            //根据边的ID获取边的信息
            IEnumEIDInfo pEnumEIDInfo = pEIDHelper.CreateEnumEIDInfo(mEnumNetEID_Edges);
            int intCount = pEnumEIDInfo.Count;
            pEnumEIDInfo.Reset();

            IEIDInfo pEIDInfo;
            IGeometry pGeometry;
            for (int i = 0; i < intCount; i++)
            {
                pEIDInfo = pEnumEIDInfo.Next();
                //获取边的几何要素
                pGeometry = pEIDInfo.Geometry;
                pNewGeometryCollection.AddGeometryCollection((IGeometryCollection)pGeometry);
            }
            return pPolyLine;
        }
        #endregion

        

        private void networkAnalysisToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //mTool = "network analysis";
            //文件路径名称,包含文件名称和路径名称
            string strPath = null;

            //定义OpenFileDialog，获取并打开地图文档
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Title = "打开MDB";
            openFileDialog.Filter = "MDB文件（*.mdb）|*.mdb";
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                strPath = openFileDialog.FileName;

            }
            else {
                return;
            }
            //修改当前工具
            mTool = "Network";
            //获取几何网络文件路径
            //注意修改此路径为当前存储路径

            //打开工作空间
            IWorkspaceFactory pWorkspaceFactory = new AccessWorkspaceFactory();
            IFeatureWorkspace pFeatureWorkspace = pWorkspaceFactory.OpenFromFile(strPath, 0) as IFeatureWorkspace;
            //获取要素数据集
            //注意名称的设置要与上面创建保持一致
            IFeatureDataset pFeatureDataset = pFeatureWorkspace.OpenFeatureDataset("high");

            //获取network集合
            INetworkCollection pNetWorkCollection = pFeatureDataset as INetworkCollection;
            //获取network的数量,为零时返回
            int intNetworkCount = pNetWorkCollection.GeometricNetworkCount;
            if (intNetworkCount < 1)
                return;
            //FeatureDataset可能包含多个network，我们获取指定的network
            //注意network的名称的设置要与上面创建保持一致
            mGeometricNetwork = pNetWorkCollection.get_GeometricNetworkByName("high_net");

            //将Network中的每个要素类作为一个图层加入地图控件
            IFeatureClassContainer pFeatClsContainer = mGeometricNetwork as IFeatureClassContainer;
            //获取要素类数量，为零时返回
            int intFeatClsCount = pFeatClsContainer.ClassCount;
            if (intFeatClsCount < 1)
                return;
            IFeatureClass pFeatureClass;
            IFeatureLayer pFeatureLayer;
            for (int i = 0; i < intFeatClsCount; i++)
            {
                //获取要素类
                pFeatureClass = pFeatClsContainer.get_Class(i);
                pFeatureLayer = new FeatureLayerClass();
                pFeatureLayer.FeatureClass = pFeatureClass;
                pFeatureLayer.Name = pFeatureClass.AliasName;
                //加入地图控件
                this.axMapControl1.AddLayer((ILayer)pFeatureLayer, 0);
            }

            //计算snap tolerance为图层最大宽度的1/100
            //获取图层数量
            int intLayerCount = this.axMapControl1.LayerCount;
            IGeoDataset pGeoDataset;
            IEnvelope pMaxEnvelope = new EnvelopeClass();
            for (int i = 0; i < intLayerCount; i++)
            {
                //获取图层
                pFeatureLayer = this.axMapControl1.get_Layer(i) as IFeatureLayer;
                pGeoDataset = pFeatureLayer as IGeoDataset;
                //通过Union获得较大图层范围
                pMaxEnvelope.Union(pGeoDataset.Extent);
            }
            double dblWidth = pMaxEnvelope.Width;
            double dblHeight = pMaxEnvelope.Height;
            double dblSnapTol;
            if (dblHeight < dblWidth)
                dblSnapTol = dblWidth * 0.01;
            else
                dblSnapTol = dblHeight * 0.01;

            //设置源地图，几何网络以及捕捉容差
            mPointToEID = new PointToEIDClass();
            mPointToEID.SourceMap = this.axMapControl1.Map;
            mPointToEID.GeometricNetwork = mGeometricNetwork;
            mPointToEID.SnapTolerance = dblSnapTol;
        }
        #region 编辑相关函数
        private void startToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.axMapControl1.MousePointer = esriControlsMousePointer.esriPointerDefault;
            
            //判断是否存在可编辑图层
            if (this.axMapControl1.Map.LayerCount == 0)
                return;
            EditingLayer editing = new EditingLayer(this.axMapControl1);
            if (this.axMapControl1.Map.LayerCount == 0)
            {
                MessageBox.Show("MapControl中未添加图层！", "提示");
                return;
            }

            if (editing.ShowDialog() == DialogResult.OK)
            {
                IMap pMap = this.axMapControl1.Map;
                IFeatureLayer pFeatureLayer = this.axMapControl1.get_Layer(editing.index) as IFeatureLayer;
                //初始化编辑
                if (mEdit == null)
                {
                    mEdit = new Edit(pFeatureLayer, pMap);
                }
                //开始编辑
                mEdit.StartEditing();
                this.startToolStripMenuItem.Enabled = false;
                this.cboTasks.Enabled = true;
                this.cboTasks.Visible = true;
                this.stopEditingToolStripMenuItem.Enabled = true;
                this.saveEditsToolStripMenuItem.Enabled = true;
            }

        }
        private void saveEditsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //判断编辑是否初始化
            if (mEdit == null)
                return;
            //处于编辑状态且已编辑则保存
            if (mEdit.IsEditing() && mEdit.HasEdited())
            {
                mEdit.SaveEditing(true);
            }
        
        }
        private void stopEditingToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (mEdit == null)
                return;
            if (mEdit.HasEdited())
            {
                DialogResult dr = MessageBox.Show("图层已编辑，是否保存？", "提示", MessageBoxButtons.OKCancel);
                if (dr == DialogResult.OK)
                    mEdit.SaveEditing(true);
                else
                    mEdit.SaveEditing(false);
            }
            //mEdit.ClearSelection();

            mEdit = null;
            this.axMapControl1.Refresh();
            this.startToolStripMenuItem.Enabled = true;
            this.cboTasks.Enabled = false;
            this.cboTasks.Visible = false;
            this.stopEditingToolStripMenuItem.Enabled = false;
            this.saveEditsToolStripMenuItem.Enabled = false;
        }
        #endregion

        private void axMapControl1_OnMapReplaced(object sender, IMapControlEvents2_OnMapReplacedEvent e)
        {
            this.axMapControl2.Map = new MapClass();
            for (int i = 0; i < this.axMapControl1.LayerCount ; i++)
            {
                this.axMapControl2.AddLayer(this.axMapControl1.get_Layer(i));
            }
            this.axMapControl2.Extent = this.axMapControl1.FullExtent;
        }

        private void OpenSxdFile_Click(object sender, EventArgs e)
        {
            //文件过滤
            mOpenFileDialog.Filter = "sxd文件|*.sxd";
            //打开文件对话框打开事件
            if (mOpenFileDialog.ShowDialog() == DialogResult.OK)
            {
                //从打开对话框中得到打开文件的全路径,并将该路径传入到mSceneControl中
                mSceneControl.LoadSxFile(mOpenFileDialog.FileName);
            }

        }

        private void OpenRasterFile_Click(object sender, EventArgs e)
        {
            string sFileName = null;
            //新建栅格图层
            IRasterLayer pRasterLayer = null;
            pRasterLayer = new RasterLayerClass();
            //取消文件过滤
            mOpenFileDialog.Filter = "所有文件|*.*";
            //打开文件对话框打开事件
            if (mOpenFileDialog.ShowDialog() == DialogResult.OK)
            {
                //从打开对话框中得到打开文件的全路径
                sFileName = mOpenFileDialog.FileName;
                //创建栅格图层
                pRasterLayer.CreateFromFilePath(sFileName);
                //将图层加入到控件中
                mSceneControl.Scene.AddLayer(pRasterLayer, true);

                //将当前视点跳转到栅格图层
                ICamera pCamera = mSceneControl.Scene.SceneGraph.ActiveViewer.Camera;
                //得到范围
                IEnvelope pEenvelop = pRasterLayer.VisibleExtent;
                //添加z轴上的范围
                pEenvelop.ZMin = mSceneControl.Scene.Extent.ZMin;
                pEenvelop.ZMax = mSceneControl.Scene.Extent.ZMax;
                //设置相机
                pCamera.SetDefaultsMBB(pEenvelop);
                mSceneControl.Refresh();
            }
        }

        private void SaveImage_Click(object sender, EventArgs e)
        {
            string sFileName = "";
            //保存对话框的标题
            mSaveFileDialog.Title = "保存图片";
            //保存对话框过滤器
            mSaveFileDialog.Filter = "BMP图片|*.bmp|JPG图片|*.jpg";
            //图片的高度和宽度
            int Width = mSceneControl.Width;
            int Height = mSceneControl.Height;
            if (mSaveFileDialog.ShowDialog() == DialogResult.OK)
            {
                sFileName = mSaveFileDialog.FileName;
                if (mSaveFileDialog.FilterIndex == 1)//保存成BMP格式的文件
                {
                    mSceneControl.SceneViewer.GetSnapshot(Width, Height,
                        esri3DOutputImageType.BMP, sFileName);
                }
                else//保存成JPG格式的文件
                {
                    mSceneControl.SceneViewer.GetSnapshot(Width, Height,
                        esri3DOutputImageType.JPEG, sFileName);
                }
                MessageBox.Show("保存图片成功！");
                mSceneControl.Refresh();
            }
        }

        private void mSceneControl_OnMouseDown(object sender, ISceneControlEvents_OnMouseDownEvent e)
        {
            if (mPointSearch.Checked)//check按钮处于打勾状态
            {
                //查询
                mSceneControl.SceneGraph.LocateMultiple(mSceneControl.SceneGraph.ActiveViewer,
                    e.x, e.y, esriScenePickMode.esriScenePickAll, false, out mHit3DSet);
                mHit3DSet.OnePerLayer();
                if (mHit3DSet == null)//没有选中对象
                {
                    MessageBox.Show("没有选中对象");
                }
                else
                {
                    //显示在ResultForm控件中。
                    mResultForm.Show();
                    mResultForm.refeshView(mHit3DSet);
                }
                mSceneControl.Refresh();
            }
        }

        private void RefreshLayer_Click(object sender, EventArgs e)
        {
            mLayerCombox.Items.Clear();
            //得到当前场景中所有图层
            int nCount = mSceneControl.Scene.LayerCount;
            if (nCount <= 0)//没有图层的情况
            {
                MessageBox.Show("场景中没有图层，请加入图层");
                return;
            }
            int i;
            ILayer pLayer = null;
            //将所有的图层的名称显示到复选框中
            for (i = 0; i < nCount; i++)
            {
                pLayer = mSceneControl.Scene.get_Layer(i);
                mLayerCombox.Items.Add(pLayer.Name);
            }
            //将复选框设置为选中第一项
            mLayerCombox.SelectedIndex = 0;
            addFieldNameToCombox(mLayerCombox.Items[mLayerCombox.SelectedIndex].ToString());
        }

        //更加图层的名字将该图层的字段加入到combox中
        private void addFieldNameToCombox(string layerName)
        {
            mFeildCombox.Items.Clear();
            int i;
            IFeatureLayer pFeatureLayer = null;
            IFields pField = null;
            int nCount = mSceneControl.Scene.LayerCount;
            ILayer pLayer = null;
            //寻找名称为layerName的FeatureLayer;
            for (i = 0; i < nCount; i++)
            {
                pLayer = mSceneControl.Scene.get_Layer(i) as IFeatureLayer;
                if (pLayer.Name == layerName)//找到了layerName的Featurelayer
                {
                    pFeatureLayer = pLayer as IFeatureLayer;
                    break;
                }
            }
            if (pFeatureLayer != null)//判断是否找到
            {
                pField = pFeatureLayer.FeatureClass.Fields;
                nCount = pField.FieldCount;
                //将该图层中所用的字段写入到mFeildCombox中去
                for (i = 0; i < nCount; i++)
                {
                    mFeildCombox.Items.Add(pField.get_Field(i).Name);
                }
            }
            mFeildCombox.SelectedIndex = 0;
        }

        private void mLayerCombox_SelectedIndexChanged(object sender, EventArgs e)
        {
            addFieldNameToCombox(mLayerCombox.Items[mLayerCombox.SelectedIndex].ToString());
        }

        private void button1_Click(object sender, EventArgs e)
        {
             if(mLayerCombox.Text == ""|| mFeildCombox.Text == "")//判断输入合法性
            {
                MessageBox.Show("没有相应的图层");
                return;
            }
            ITinEdit pTin = new TinClass();
            //寻找Featurelayer
            IFeatureLayer pFeatureLayer =
                mSceneControl.Scene.get_Layer(mLayerCombox.SelectedIndex) as IFeatureLayer;
            if (pFeatureLayer != null)
            {
                IEnvelope pEnvelope = new EnvelopeClass();
                IFeatureClass pFeatureClass = pFeatureLayer.FeatureClass;
                IQueryFilter pQueryFilter = new QueryFilterClass();
                IField pField = null;
                //找字段
                pField = pFeatureClass.Fields.get_Field(pFeatureClass.Fields.FindField(mFeildCombox.Text));
                if (pField.Type == esriFieldType.esriFieldTypeInteger ||
                     pField.Type == esriFieldType.esriFieldTypeDouble ||
                     pField.Type == esriFieldType.esriFieldTypeSingle)//判断类型
                {
                    IGeoDataset pGeoDataset = pFeatureLayer as IGeoDataset;
                    pEnvelope = pGeoDataset.Extent;
                    //设置空间参考系
                    ISpatialReference pSpatialReference;
                    pSpatialReference = pGeoDataset.SpatialReference;
                    //选择生成TIN的输入类型
                    esriTinSurfaceType pSurfaceTypeCount = esriTinSurfaceType.esriTinMassPoint;
                    switch (mTINType.Text)
                    {
                        case "点":
                            pSurfaceTypeCount = esriTinSurfaceType.esriTinMassPoint;
                            break;
                        case "直线":
                            pSurfaceTypeCount = esriTinSurfaceType.esriTinSoftLine;
                            break;
                        case "光滑线":
                            pSurfaceTypeCount = esriTinSurfaceType.esriTinHardLine;
                            break;
                    }
                    //创建TIN
                    pTin.InitNew(pEnvelope);
                    object missing = Type.Missing;
                    //生成TIN
                    pTin.AddFromFeatureClass(pFeatureClass, pQueryFilter, pField, pField, pSurfaceTypeCount, ref missing);
                    pTin.SetSpatialReference(pGeoDataset.SpatialReference);
                    //创建Tin图层并将Tin图层加入到场景中去
                    ITinLayer pTinLayer = new TinLayerClass();
                    pTinLayer.Dataset = pTin as ITin;
                    mSceneControl.Scene.AddLayer(pTinLayer, true);
                }
                else
                {
                    MessageBox.Show("该字段的类型不符合构建TIN的条件");
                }
            }
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Dispose();
        }

        private void MyGIS_Load(object sender, EventArgs e)
        {
            //加载编辑任务
            cboTasks.Items.Add("新建");
            cboTasks.Items.Add("移动");
            cboTasks.SelectedIndex = 0;

            //开始编辑之前，将编辑按钮设为不可用
            this.cboTasks.Enabled = false;
            this.saveEditsToolStripMenuItem.Enabled = false;
            this.stopEditingToolStripMenuItem.Enabled = false;

        }

        private void editToolStripMenuItem_Click(object sender, EventArgs e)
        {
            mTool = "Edit";
            this.axMapControl1.MousePointer = esriControlsMousePointer.esriPointerDefault;

        }

        private void toolStripMenuItem2_Click(object sender, EventArgs e)
        {
            this.axMapControl1.MousePointer = esriControlsMousePointer.esriPointerDefault;
        }

        private void geoprocessingToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.axMapControl1.MousePointer = esriControlsMousePointer.esriPointerDefault;

        }


    }
}
