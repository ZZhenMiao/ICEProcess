using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace ICE_BackEnd
{
    public partial class MainWindow : Window
    {
        private static int conInfoSelectedIndex;

        private static ConnectProperty DatabaseIP_prop = new ConnectProperty { Header = "数据库IP地址：" };
        private static ConnectProperty LocalHostIP_prop = new ConnectProperty { Header = "服务器IP地址" };
        private static ConnectProperty StartUpTime_prop = new ConnectProperty { Header = "本次启动时间" };
        private static ConnectProperty RunningTime_prop = new ConnectProperty { Header = "本次已运行" };
        private static ConnectProperty State_prop = new ConnectProperty { Header = "当前状态" };
        private static ConnectProperty ConnectionNum_prop = new ConnectProperty { Header = "当前连接数" };
        private static ConnectProperty ReceiveNum_prop = new ConnectProperty { Header = "已接收指令数" };
        private static ConnectProperty ReceiveNumMean_prop = new ConnectProperty { Header = "平均接收指令数" };
        private static ConnectProperty QueryNum_prop = new ConnectProperty { Header = "执行查询指令数" };
        private static ConnectProperty QueryNumMean_prop = new ConnectProperty { Header = "平均查询指令数" };
        private static ConnectProperty WriteNum_prop = new ConnectProperty { Header = "执行写入指令数" };
        private static ConnectProperty WriteNumMean_prop = new ConnectProperty { Header = "平均写入指令数" };
        private static ConnectProperty SendNum_prop = new ConnectProperty { Header = "已发送指令数" };
        private static ConnectProperty SendNumMean_prop = new ConnectProperty { Header = "平均发送指令数" };
        private static ConnectProperty NowLoad_prop = new ConnectProperty { Header = "当前负载" };
        private static ConnectProperty MaxLoad_prop = new ConnectProperty { Header = "峰值负载" };

        private ObservableCollection<ConnectProperty> ConnectProperties { get; } = new ObservableCollection<ConnectProperty>{
            DatabaseIP_prop,LocalHostIP_prop,StartUpTime_prop,RunningTime_prop,State_prop,
            ConnectionNum_prop,ReceiveNum_prop,ReceiveNumMean_prop,QueryNum_prop,QueryNumMean_prop,
            WriteNum_prop,WriteNumMean_prop,SendNum_prop,SendNumMean_prop,NowLoad_prop,MaxLoad_prop};

    }
}
