using LiZhenMySQL;
using System;
using System.Collections.ObjectModel;

namespace ICE_Model
{
    /// <summary>
    /// 流程
    /// </summary>
    [Serializable]
    public class Process : DbNamedObject, IDbObject
    {
        public NameExpression NameExpression { get; set; }

        public ObservableCollection<Module> Modules { get; } = new ObservableCollection<Module>();
        public ObservableCollection<ModuleChain> ModuleChains { get; } = new ObservableCollection<ModuleChain>();
    }

    /// <summary>
    /// 任务
    /// </summary>
    public class Task : DbNamedObject, IDbObject
    {
        public Module Module { get; set; }
        public Process Process { get; set; }

        public ObservableCollection<AssetType> MeansOfProduction { get; } = new ObservableCollection<AssetType>();
        public ObservableCollection<AssetType> Production { get; } = new ObservableCollection<AssetType>();

        public ObservableCollection<TaskState> TaskStates { get; } = new ObservableCollection<TaskState>();
        public ObservableCollection<Field<Task>> Fields { get; } = new ObservableCollection<Field<Task>>();
        public ObservableCollection<Step> Steps { get; } = new ObservableCollection<Step>();
    }
    /// <summary>
    /// 任务信息   
    /// </summary>
    public class TaskInfo
    {
        public Task Task { get; set; }
        public Project Project { get; set; }
        public ObservableCollection<FieldValue<Field<Task>>> FieldValues { get; } = new ObservableCollection<FieldValue<Field<Task>>>();
        public TaskState TaskState { get; set; }
    }
    /// <summary>
    /// 步骤
    /// </summary>
    public class Step : DbNamedObject, IDbObject
    {
        public int ID_Task { get; set; }
        public int ID_LastTask { get; set; }
        public int ID_NestTask { get; set; }

        public bool Order { get; set; }
        public string OrderContent { get; set; }
        public ReviewMode ReviewMode { get; set; }

        public ObservableCollection<StepState> StepStates { get; } = new ObservableCollection<StepState>();
        public ObservableCollection<Obligation> WorkObligations { get; } = new ObservableCollection<Obligation>();
        public ObservableCollection<Obligation> OrderObligations { get; } = new ObservableCollection<Obligation>();
    }
    /// <summary>
    /// 审核方式
    /// </summary>
    public enum ReviewMode { Single, Parallel }
    /// <summary>
    /// 状态类型
    /// </summary>
    public enum StateType { Unstart, Waiting, Working, Retake, Approve, Finished, Ignore, Closed, Stoped }
    /// <summary>
    /// 状态抽象基类
    /// </summary>
    public abstract class State : DbNamedObject, IDbObject
    {
        public StateType StateType { get; set; }
    }
    /// <summary>
    /// 任务状态
    /// </summary>
    public class TaskState : State
    {

    }
    /// <summary>
    /// 步骤状态
    /// </summary>
    public class StepState : State
    {

    }

    public class StepInfo
    {
        public int ID_Step { get; set; }
        public int ID_StepState { get; set; }
        public StepState StepState { get; set; }
    }
}
