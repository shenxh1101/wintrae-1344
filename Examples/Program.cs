/* ================================================================
 * 控制台程序入口：运行全部示例
 * ================================================================ */

using System;
using FinanceReimbursement.Examples;

namespace FinanceReimbursement.Runner
{
    public class Program
    {
        public static void Main(string[] args)
        {
            try
            {
                Console.WriteLine("╔══════════════════════════════════════════════════════════╗");
                Console.WriteLine("║        FinanceReimbursement 财务报销类库 - 演示程序      ║");
                Console.WriteLine("╚══════════════════════════════════════════════════════════╝\n");

                Console.WriteLine("【示例 1】从建单到打印数据 —— 完整正常流程");
                Console.WriteLine("———————————————————————————————————————————————————————————\n");
                QuickStartExample.Run();

                Console.WriteLine("\n\n");
                Console.WriteLine("【示例 2】各类校验错误的用户友好展示");
                Console.WriteLine("———————————————————————————————————————————————————————————\n");
                ValidationShowcaseExample.Run();

                Console.WriteLine("\n\n");
                Console.WriteLine("【示例 3】企业级接入：仓储+规则方案+批量处理+增强审批");
                Console.WriteLine("———————————————————————————————————————————————————————————\n");
                EnterpriseIntegrationExample.Run();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n❌ 运行异常：{ex.Message}");
                Console.WriteLine(ex.StackTrace);
            }

            Console.WriteLine("\n按任意键退出...");
            Console.ReadKey();
        }
    }
}
