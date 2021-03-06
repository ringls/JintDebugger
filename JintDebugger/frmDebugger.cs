using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using ICSharpCode.TextEditor;
using ICSharpCode.TextEditor.Document;
using Jint;
using Jint.Native;
using Jint.Parser;
using Jint.Runtime;
using Jint.Runtime.Debugger;

namespace JintDebugger
{
    public partial class frmDebugger : Form
    {
        /// <summary>
        /// 调试器
        /// </summary>
        Debugger db;

        public frmDebugger()
        {

            db = new Debugger();

            db.OnBegin += Db_OnBegin;
            db.OnStep += Db_OnStep;//步进事件
            db.OnEnd += Db_OnEnd;//结束事件
            db.OnError += Db_OnError;//出错事件

            InitializeComponent();

            //this.dgInfo.AutoGenerateColumns = false;

            SetControlsStatus(false);
            //自定义代码高亮
            this.txtCode.SetHighlighting("JavaScript");

            //插入断点
            this.txtCode.ActiveTextAreaControl.TextArea.IconBarMargin.MouseDown += IconBarMargin_MouseDown;
            //初始脚本
            this.txtCode.Document.TextContent = @"function exec(){
    var z = [];
    z.push({test:'123',ID:4})
    z.push({test:'120',ID:1})
    var i=0;
    var j = 1;
    //执行相加
    return add(i,j);
}

function add(x,y){
    return x+y;
}";

            //初始化标题
            this.Text = string.Format("{0}({1})", this.Text, System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString());

        }



        private void Db_OnBegin(object obj)
        {
            SetControlsStatus(true);
        }

        private void Db_OnError(object obj, JavaScriptException e)
        {
            SetOutPut(string.Format("{0}脚本运行出错，第{2}行第{3}列,错误信息：{1}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), e.Message, e.LineNumber, e.Column));
            //将运行标签去除
            SetCaretMark(null);
            SetControlsStatus(false);
        }

        private void Db_OnEnd(object obj, object output)
        {
            //将输出显示在文本框中
            var jsonResult = Newtonsoft.Json.JsonConvert.SerializeObject(output);
            SetOutPut(string.Format("{0}脚本运行结束，输出：{1}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), jsonResult));
            //将运行标签去除
            SetCaretMark(null);
            SetControlsStatus(false);
        }

        private void Db_OnStep(object obj, DebugInformation e)
        {
            ShowDebugInfo(e);
        }

        private void ShowDebugInfo(DebugInformation e)
        {
            //将数据展示在监视中
            IList<LocalVariable> list = new List<LocalVariable>();
            foreach (var item in e.Locals)
            {
                if (item.Key != "arguments")
                {
                    list.Add(GetLocalVariableJS(item.Key, item.Value));
                }
                //if (item.Value.IsObject() == true)
                //{
                //    list.Add(new LocalVariable() { Name = item.Key, TypeName = item.Value.Type.ToString(), Value = item.Value.ToString(), Obj = item.Value.ToObject() });

                //}
                //else
                //{
                //    list.Add(new LocalVariable() { Name = item.Key, TypeName = item.Value.Type.ToString(), Value = item.Value.ToString() });

                //}
                /*
         if (item.Value.IsObject() == true)
         {
             var obj = item.Value.ToObject();
             list.Add(new LocalVariable() { Name = item.Key, TypeName = item.Value.Type.ToString(), Value = Newtonsoft.Json.JsonConvert.SerializeObject(obj,) });

             var obj = item.Value.ToObject();
             var type = obj.GetType();
             //是否数组
             if (type.IsGenericType)
             {
                 //判断长度
                 int count = Convert.ToInt32(type.GetProperty("Count").GetValue(obj, null));
                 if (count > 0)
                 {
                     var subObject = type.GetProperty("Item").GetValue(obj, new object[] { 0 });
                     var subType = subObject.GetType();
                     if (subType.Name == "DapperRow" || subType.Name == "ExpandoObject")
                     {
                         list.Add(new LocalVariable() { Name = item.Key, TypeName = item.Value.Type.ToString(), Value = Newtonsoft.Json.JsonConvert.SerializeObject(obj) });
                     }
                     else
                     {
                         list.Add(new LocalVariable() { Name = item.Key, TypeName = item.Value.Type.ToString(), Value = item.Value.ToString() });
                     }
                 }
                 else
                 {
                     list.Add(new LocalVariable() { Name = item.Key, TypeName = item.Value.Type.ToString(), Value = item.Value.ToString() });
                 }
             }
             else if (type.Name == "ExpandoObject")
             {
                 list.Add(new LocalVariable() { Name = item.Key, TypeName = item.Value.Type.ToString(), Value = Newtonsoft.Json.JsonConvert.SerializeObject(obj) });
             }
             else
             {
                 list.Add(new LocalVariable() { Name = item.Key, TypeName = item.Value.Type.ToString(), Value = item.Value.ToString() });
             }

         }
         else
         {
             list.Add(new LocalVariable() { Name = item.Key, TypeName = item.Value.Type.ToString(), Value = item.Value.ToString() });
         }*/
            }
            //将入参放入监视列表中
            foreach (var item in e.Globals)
            {
                if (item.Key == "model")
                {
                    list.Add(GetLocalVariableJS(item.Key, item.Value));
                }
            }

            ShowLocalVariableList(list);

            //获取文本位置
            int start = GetOffset(e.CurrentStatement.Location.Start);
            int end = GetOffset(e.CurrentStatement.Location.End);

            var position = this.txtCode.Document.OffsetToPosition(start);

            var caretMark = new CaretMark(this.txtCode.Document, position);
            //展示当前运行行
            SetCaretMark(caretMark);
        }

        private LocalVariable GetLocalVariableJS(string key, JsValue value)
        {
            var result = new LocalVariable() { Name = key, TypeName = value.Type.ToString() };
            if (value.IsArray())
            {
                var trueValue = value.AsArray();
                if (trueValue.GetLength() > 0)
                {
                    var props = trueValue.GetOwnProperties();
                    foreach (var item in props)
                    {
                        var ch = GetLocalVariableJS(item.Key, item.Value.Value);
                        result.Children.Add(ch);
                    }
                }
                //result.Value=trueValue.
            }
            else if (value.IsBoolean())
            {
                var trueValue = value.AsBoolean();
                result.Value = trueValue.ToString();
            }
            else if (value.IsDate())
            {
                var trueValue = value.AsDate();
                result.Value = trueValue.ToDateTime().ToString("yyyy-MM-dd HH:mm:ss");
            }
            else if (value.IsNumber())
            {
                var trueValue = value.AsNumber();
                result.Value = trueValue.ToString();
            }
            else if (value.IsString())
            {
                var trueValue = value.AsString();
                result.Value = trueValue;
            }
            else if (value.IsObject())
            {
                var trueValue = value.AsObject();
                //判断对象是否原生JS              
                if (trueValue.GetType().Name == "ObjectInstance")
                {
                    var props = trueValue.GetOwnProperties();
                    foreach (var item in props)
                    {
                        var ch = GetLocalVariableJS(item.Key, item.Value.Value);
                        result.Children.Add(ch);
                    }
                }
                else
                {
                    var csharpValue = value.ToObject();
                    if (csharpValue != null)
                    {
                        //只处理DapperRow
                        result = GetLocalVariableCSharp(key, csharpValue);
                        //csharpValue.
                    }
                }
            }
            return result;
        }

        private LocalVariable GetLocalVariableCSharp(string key, object obj)
        {
            if (obj != null)
            {
                var type = obj.GetType();
                var result = new LocalVariable() { Name = key, TypeName = type.Name };
                //是否泛型
                if (type.IsGenericType)
                {
                    //判断长度
                    int count = Convert.ToInt32(type.GetProperty("Count").GetValue(obj, null));
                    if (count > 0)
                    {
                        for (var i = 0; i < count; i++)
                        {
                            var subObject = type.GetProperty("Item").GetValue(obj, new object[] { i });
                            result.Children.Add(GetLocalVariableCSharp(i.ToString(), subObject));
                        }
                    }
                }
                else if (type.Name == "DapperRow" || type.Name == "ExpandoObject")
                {
                    var dict = (IDictionary<string, object>)obj;
                    foreach (var prop in dict)
                    {
                        result.Children.Add(GetLocalVariableCSharp(prop.Key, prop.Value));
                    }
                }
                else if (type.Name == "String" || type.Name == "Double" || type.Name == "Int32" || type.Name == "DateTime")
                {
                    result.Value = obj.ToString();
                }

                return result;
            }
            else
            {
                return new LocalVariable() { Name = key, TypeName = "Undefined" };
            }
        }

        private void IconBarMargin_MouseDown(AbstractMargin sender, Point mousepos, MouseButtons mouseButtons)
        {
            if (mouseButtons != MouseButtons.Left)
                return;

            var textArea = this.txtCode.ActiveTextAreaControl.TextArea;

            int yPos = mousepos.Y;
            int lineHeight = textArea.TextView.FontHeight;
            int lineNumber = (yPos + textArea.VirtualTop.Y) / lineHeight;

            //是否已经存在该断点
            //if(this.txtCode.Document.BookmarkManager.Marks)

            //if (BreakPoints.Any(p => p.Line == lineNumber + 1) || lineNumber >= textArea.Document.TotalNumberOfLines)
            //    return;


            string text = textArea.Document.GetText(textArea.Document.GetLineSegment(lineNumber));
            int offset = -1;

            for (int i = 0; i < text.Length; i++)
            {
                if (
                    !Char.IsWhiteSpace(text[i]) &&
                    text[i] != '/'
                )
                {
                    offset = i;
                    break;
                }
            }

            if (offset == -1)
                return;

            var breakPoint = new EditorBreakPoint(lineNumber + 1, offset);

            db.AddBreakPoint(breakPoint);

            var document = this.txtCode.Document;

            var mark = new BreakPointMark(
                document,
                new TextLocation(
                    offset,
                    lineNumber
                )
            );

            mark.Removed += (s, e) =>
            {
                db.RemoveBreakPoint(breakPoint);
            };

            document.BookmarkManager.AddMark(mark);

            this.txtCode.Refresh();
        }

        private int GetOffset(Position location)
        {
            return this.txtCode.Document.PositionToOffset(new TextLocation(
                location.Column, location.Line - 1
            ));
        }

        object locker = new object();
        #region 设置输出
        delegate void SetOutPutCallback(string text);
        private void SetOutPut(string text)
        {
            if (this.txtOutPut.InvokeRequired)//如果调用控件的线程和创建创建控件的线程不是同一个则为True
            {
                while (!this.txtOutPut.IsHandleCreated)
                {
                    //解决窗体关闭时出现“访问已释放句柄“的异常
                    if (this.txtOutPut.Disposing || this.txtOutPut.IsDisposed)
                        return;
                }
                SetOutPutCallback d = new SetOutPutCallback(SetOutPut);
                this.txtOutPut.Invoke(d, new object[] { text });
            }
            else
            {
                lock (locker)
                {
                    if (this.txtOutPut.Lines.Length > 30)
                    {
                        this.txtOutPut.Clear();
                    }
                    this.txtOutPut.AppendText(text + "\r\n");
                    this.txtOutPut.ScrollToCaret();
                }
            }
        }

        #endregion

        #region 设置运行位置
        CaretMark _caretMark;
        delegate void SetCaretMarkCallback(CaretMark caretMark);
        private void SetCaretMark(CaretMark caretMark)
        {
            if (this.txtCode.InvokeRequired)//如果调用控件的线程和创建创建控件的线程不是同一个则为True
            {
                while (!this.txtCode.IsHandleCreated)
                {
                    //解决窗体关闭时出现“访问已释放句柄“的异常
                    if (this.txtCode.Disposing || this.txtCode.IsDisposed)
                        return;
                }
                var d = new SetCaretMarkCallback(SetCaretMark);
                this.txtCode.Invoke(d, new object[] { caretMark });
            }
            else
            {
                lock (locker)
                {
                    if (caretMark == null)
                    {
                        if (_caretMark != null)
                        {
                            this.txtCode.Document.BookmarkManager.RemoveMark(_caretMark);
                            _caretMark = null;
                            this.txtCode.Refresh();
                        }
                    }
                    else
                    {
                        //清空当前的书签标识
                        if (_caretMark != null)
                        {
                            this.txtCode.Document.BookmarkManager.RemoveMark(_caretMark);
                        }
                        _caretMark = caretMark;
                        this.txtCode.Document.BookmarkManager.AddMark(_caretMark);
                        this.txtCode.Refresh();
                    }
                }
            }
        }
        #endregion

        #region 设置监视
        delegate void ShowLocalVariableListCallback(IList<LocalVariable> list);
        private void ShowLocalVariableList(IList<LocalVariable> list)
        {
            if (this.lvInfo.InvokeRequired)//如果调用控件的线程和创建创建控件的线程不是同一个则为True
            {
                while (!this.lvInfo.IsHandleCreated)
                {
                    //解决窗体关闭时出现“访问已释放句柄“的异常
                    if (this.lvInfo.Disposing || this.lvInfo.IsDisposed)
                        return;
                }
                var d = new ShowLocalVariableListCallback(ShowLocalVariableList);
                this.lvInfo.Invoke(d, new object[] { list });
            }
            else
            {
                lock (locker)
                {
                    //this.dgInfo.DataSource = list;
                    this.lvInfo.Roots = list;
                    this.lvInfo.Refresh();
                }
            }
        }
        #endregion

        #region 设置控件状态
        delegate void SetControlsStatusCallback(bool status);

        private void SetControlsStatus(bool status)
        {
            if (this.InvokeRequired)//如果调用控件的线程和创建创建控件的线程不是同一个则为True
            {
                while (!this.IsHandleCreated)
                {
                    //解决窗体关闭时出现“访问已释放句柄“的异常
                    if (this.Disposing || this.IsDisposed)
                        return;
                }
                SetControlsStatusCallback d = new SetControlsStatusCallback(SetControlsStatus);
                this.Invoke(d, new object[] { status });
            }
            else
            {
                lock (locker)
                {
                    this.btnRun.Enabled = !status;
                    this.btnPause.Enabled = status;
                    this.btnStop.Enabled = status;
                    this.txtCode.IsReadOnly = status;
                    this.btnStepInto.Enabled = status;
                    this.btnStepOver.Enabled = status;
                    this.btnStepOut.Enabled = status;
                }
            }
        }
        #endregion

        private void btnRun_Click(object sender, EventArgs e)
        {
            //入参判断是否json

            //var thread = new Thread(() => ThreadProc(this.txtCode.Text, BreakPoints));
            //CurrentDebugStatus = 0;
            //thread.Start();
            if (db.IsRunning)
            {
                //db.StepOut();
            }
            else
            {
                //将document中的breakpoint转成breakPointList
                IList<EditorBreakPoint> BreakPoints = new List<EditorBreakPoint>();
                foreach (var item in this.txtCode.Document.BookmarkManager.Marks)
                {
                    if (item.GetType().Name == "BreakPointMark")
                    {
                        BreakPointMark breakPointMark = (BreakPointMark)item;
                        BreakPoints.Add(new EditorBreakPoint(breakPointMark.Location.Line + 1, breakPointMark.Location.Column));
                    }
                }
                var instance = new JintDebugInstance()
                {
                    Script = this.txtCode.Text,
                    BreakPoints = BreakPoints,
                    InputJson = this.txtInput.Text.Trim()
                };
                db.Run(instance);
            }
        }

        private void btnStepInto_Click(object sender, EventArgs e)
        {
            db.StepInto();
            //db.Step(StepMode.Into);
        }

        private void btnStepOver_Click(object sender, EventArgs e)
        {
            db.StepOver();
            //db.Step(StepMode.Over);
        }

        private void btnStepOut_Click(object sender, EventArgs e)
        {
            db.StepOut();
            //db.Step(StepMode.Out);
        }


        private void btnPause_Click(object sender, EventArgs e)
        {
            db.Pause();
        }

        private void btnStop_Click(object sender, EventArgs e)
        {
            db.Stop();
        }

        private void frmDebugger_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Shift && e.KeyCode == Keys.F5)
            {
                e.Handled = true;
                btnStop.PerformClick();
            }
            else if (e.Shift && e.KeyCode == Keys.F11)
            {
                e.Handled = true;
                btnStepOut.PerformClick();
            }
            else if (e.KeyCode == Keys.F5)
            {
                e.Handled = true;
                btnRun.PerformClick();
            }
            else if (e.KeyCode == Keys.F11)
            {
                e.Handled = true;
                btnStepInto.PerformClick();
            }
        }

        private void dgInfo_RowEnter(object sender, DataGridViewCellEventArgs e)
        {
            //LocalVariable item = dgInfo.Rows[e.RowIndex].DataBoundItem as LocalVariable;
            //if (item.Obj != null)
            //{
            //    pgItem.SelectedObject = item.Obj;
            //}
        }

        private void frmDebugger_Load(object sender, EventArgs e)
        {
            this.tabPage1.Show();
            this.tabPage2.Show();
            this.tabPage3.Show();

            //是否允许展开
            this.lvInfo.CanExpandGetter = delegate (object x)
            {
                return ((LocalVariable)x).Children.Count > 0;
            };
            //可以展开的获取此条数据的子集。
            this.lvInfo.ChildrenGetter = delegate (object x)
            {
                try
                {
                    return ((LocalVariable)x).Children;
                }
                catch (UnauthorizedAccessException ex)
                {
                    this.BeginInvoke((MethodInvoker)delegate ()
                    {
                        this.lvInfo.Collapse(x);
                        MessageBox.Show(this, ex.Message, "ObjectListViewDemo", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                    });
                    return new List<LocalVariable>();
                }
            };

        }
    }
}
