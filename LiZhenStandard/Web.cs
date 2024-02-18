using System;
using System.Net;
using System.Text;

namespace LiZhenStandard
{
    public class Web
    {
        /// <summary>
        /// 获取网页信息
        /// </summary>
        /// <param name="url">完整的网址</param>
        /// <param name="encoding">Html字符串</param>
        /// <returns></returns>
        public static string GetWebPageHtml(string url, Encoding encoding = null)
        {

            try
            {
                WebClient MyWebClient = new WebClient();
                MyWebClient.Credentials = CredentialCache.DefaultCredentials;//获取或设置用于向Internet资源的请求进行身份验证的网络凭据
                Byte[] pageData = MyWebClient.DownloadData(url); //从指定网站下载数据
                //string pageHtml = Encoding.UTF8.GetString(pageData); //如果获取网站页面采用的是UTF-8，则使用这句
                return encoding is null ? Encoding.Default.GetString(pageData) : encoding.GetString(pageData);
            }
            catch (WebException webEx)
            {
                return webEx.Message;
            }
        }
    }
}
