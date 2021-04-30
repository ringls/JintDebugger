using Jint;
using Jint.Runtime;
using Jint.Runtime.Debugger;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading;
using System.Diagnostics;

namespace JintDebugger
{
    public class Debugger
    {
        public Debugger()
        {
            IsRunning = false;
        }
        /// <summary>
        /// 是否正在运行
        /// </summary>
        public bool IsRunning { get; private set; }
        private readonly object _syncRoot = new object();
        /// <summary>
        /// 是否在下一次命中断点时步进
        /// </summary>
        private bool BreakOnNextStatement = false;
        /// <summary>
        /// 运行类型
        /// </summary>
        private BreakType _breakType = BreakType.Break;
        /// <summary>
        /// 进程通知
        /// </summary>
        private AutoResetEvent _event = new AutoResetEvent(false);
        /// <summary>
        /// 步进方式
        /// </summary>
        private StepMode _stepMode;

        /// <summary>
        /// 脚本引擎
        /// </summary>
        Engine engine;
        /// <summary>
        /// 添加断点
        /// </summary>
        /// <param name="breakPoint"></param>
        /// <returns></returns>
        public void AddBreakPoint(EditorBreakPoint breakPoint)
        {
            if (engine != null)
            {
                if (engine.BreakPoints.Where(m => m.Line == breakPoint.Line).Count() == 0)
                {
                    engine.BreakPoints.Add(new BreakPoint(breakPoint.Line, breakPoint.Column));
                }
            }
        }
        /// <summary>
        /// 删除断点
        /// </summary>
        /// <param name="breakPoint"></param>
        /// <returns></returns>
        public void RemoveBreakPoint(EditorBreakPoint breakPoint)
        {
            if (engine != null)
            {
                var currentBreakPoint = engine.BreakPoints.Where(m => m.Line == breakPoint.Line).FirstOrDefault();
                if (currentBreakPoint != null)
                {
                    engine.BreakPoints.Remove(currentBreakPoint);
                }
            }
        }
        Thread thread = null;
        /// <summary>
        /// 运行
        /// </summary>
        /// <param name="Script"></param>
        /// <param name="BreakPoints"></param>
        public void Run(JintDebugInstance instance)
        {
            if (IsRunning == false)
            {
                thread = new Thread(() => ThreadProc(instance));
                _breakType = BreakType.Break;
                thread.Start();
                if (this.OnBegin != null)
                {
                    this.OnBegin.Invoke(this);
                }
            }
        }


        public void StepInto()
        {
            if (IsRunning == true)
            {
                _stepMode = StepMode.Into;
                _breakType = BreakType.Step;
                BreakOnNextStatement = true;
                _event.Set();
            }
        }

        public void StepOver()
        {
            if (IsRunning == true)
            {
                _stepMode = StepMode.Over;
                _breakType = BreakType.Step;
                BreakOnNextStatement = true;
                _event.Set();
            }
        }

        public void StepOut()
        {
            if (IsRunning == true)
            {
                _stepMode = StepMode.Out;
                _breakType = BreakType.Break;
                BreakOnNextStatement = false;
                _event.Set();
            }
        }

        public void Stop()
        {
            if (IsRunning == true)
            {
                if (thread != null)
                {
                    thread.Abort();
                    thread = null;
                }
                IsRunning = false;
                BreakOnNextStatement = false;
                if (this.OnEnd != null)
                {
                    this.OnEnd.Invoke(this, null);
                }
            }
        }

        public void Pause()
        {
            if (IsRunning == true)
            {
                _breakType = BreakType.Break;
                BreakOnNextStatement = true;
            }
        }

        /// <summary>
        /// 启动调试进程
        /// </summary>
        /// <param name="script"></param>
        /// <param name="breakPoints"></param>
        private void ThreadProc(JintDebugInstance instance)
        {
            IsRunning = true;
            try
            {
                _breakType = BreakType.Break;
                var engine = new Engine(cfg => cfg.AllowClr(typeof(DataSet).Assembly).CatchClrExceptions().DebugMode());

          
                if (instance.InputJson != "")
                {
                    engine.SetValue("strjson", instance.InputJson);
                    engine.Execute("var model=JSON.parse(strjson);");
                }
           
       
                foreach (var bp in instance.BreakPoints)
                {
                    engine.BreakPoints.Add(new BreakPoint(bp.Line, bp.Column));
                }
                var func = engine.Execute(instance.Script).GetValue("exec");
                engine.Break += Engine_Break;
                engine.Step += Engine_Step;

                var result = func.Invoke().ToObject();
                if (this.OnEnd != null)
                {
                    this.OnEnd.Invoke(this, result);
                }
            }
            catch (JavaScriptException ex)
            {
                if (this.OnError != null)
                {
                    this.OnError.Invoke(this, ex);
                }
            }
            finally
            {
                engine = null;
                IsRunning = false;
            }
        }

     

        private StepMode Engine_Break(object sender, DebugInformation e)
        {
            return ProcessStep(sender, e);
        }

        private StepMode Engine_Step(object sender, DebugInformation e)
        {
            return ProcessStep(sender, e);
        }

        private StepMode ProcessStep(object sender, DebugInformation e)
        {
            lock (_syncRoot)
            {
                if (_breakType == BreakType.Step && !BreakOnNextStatement)
                    return StepMode.None;

                BreakOnNextStatement = false;

                if (this.OnStep != null)
                {
                    OnStep.Invoke(this, e);
                }
                _event.WaitOne();
                return _stepMode;
            }
        }
        //声明一个运行开始的委托
        public delegate void JintBeginDelegate(object obj);
        //建立一个运行开始事件
        public event JintBeginDelegate OnBegin;
        //声明一个运行结束的委托
        public delegate void JintEndDelegate(object obj, object output);
        //建立一个运行结束事件
        public event JintEndDelegate OnEnd;
        //声明一个步进委托
        public delegate void JintStepDelegate(object obj, DebugInformation e);
        //建立一个步进事件
        public event JintStepDelegate OnStep;
        //声明一个错误委托
        public delegate void JintErrorDelegate(object obj, JavaScriptException e);
        //建立一个错误事件
        public event JintErrorDelegate OnError;

    }

}
