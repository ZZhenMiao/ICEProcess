using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ICE_Model;
using LiZhenStandard.Extensions;

namespace ICE_AssetLibrary
{
    public partial class MainWindow
    {
        void Query()
        {
            BaseQuery(() =>
            {
                Asset[] re_Assets = OnlyKeywords() ? QueryAssets(true) : QueryAssets(false);

                List<AssetFileInfo> re_files = new List<AssetFileInfo>();
                AnalysisKeyWords(out string sp,out string reg,out string[] sws);
                IEnumerable<IQueryCriteria> keywords = this.ActiveQueryScheme.QueryCriterias.Where(a => a is QueryCriteria_Keyword);
                re_files = AnalysisAssets(re_Assets, sp, reg, sws);
                re_files.RemoveAll(x => x is null || App.ExcludeAssetExtensions.Contains(Path.GetExtension(x.FullName)));
                re_files = re_files.GroupBy(x => x).Select(group => group.Key).ToList();
                return re_files.ToArray();
            });
        }

        void AnalysisKeyWords(out string sp,out string reg,out string[] specialWords)
        {
            IEnumerable<IQueryCriteria> keywords = this.ActiveQueryScheme.QueryCriterias.Where(a => a is QueryCriteria_Keyword);
            sp = "*"; reg = ".*";List<string> specialWords_list = new List<string>();
            foreach (IQueryCriteria keyword in keywords)
            {
                if (keyword.Name.IsMatch(@"(?<=\{).+(?=\})", out string specs))
                {
                    specialWords_list.Add(specs);
                }
                else
                {
                    sp += keyword.Name + "*";
                    reg += keyword.Name + ".*";
                }
            }
            specialWords= specialWords_list.ToArray();
        }

        void BaseQuery(QueryDelegate queryDelegate)
        {
            var timeA = DateTime.Now;
            AssetFileInfo[] re_files = queryDelegate.Invoke(); 
            this.AssetUsageLogs.Statistics(re_files);
            SortFiles(re_files);
            var timeB = DateTime.Now;
            Console.WriteLine("本次查询共用时：" + (timeB - timeA).ToString(@"mm\:ss\:fff")); ;
            if (useCache)
                Console.WriteLine("**注意：以上查询基于本地缓存文件**");

        }
    }
    public delegate AssetFileInfo[] QueryDelegate();
}
