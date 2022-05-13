using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using Discord.Commands;
using System.Linq;
using Discord;
using NPOI.SS.UserModel;
using System.Drawing;
using System.Drawing.Text;

namespace SmartFalcon_Online
{
    public class JankenRankData
    {
        public string name { get; set; }
        public int point { get; set; }
    }

    public class IppatsuIkuseiHai
    {
        public List<string> nameList;
        public List<int> scoreList;

        public bool isStart { get; set; }

        public int raceCount { get; set; }
    }


    class Program
    {
        static void Main(string[] args) => ConfigureHostBuider(args).Build().Run();

        public static IHostBuilder ConfigureHostBuider(string[] args) => Host.CreateDefaultBuilder(args)
            .ConfigureLogging(b =>
            {
                b.AddConsole();
            })
            .ConfigureServices((hostContext, services) =>
            {
                services.AddHostedService<DiscordBotService>();
            });
    }

    class DiscordBotService : BackgroundService
    {
        private DiscordSocketClient _client;
        private readonly IConfiguration _configuration;
        //ロールでのメンションに反応できるようにするため
        public readonly ulong otherID = 944029368584388701;
        //サーバーID
        public readonly ulong serverID = 842810363304869909;

        //静かにするモードか
        private bool isSilent = false;

        //呼び名リスト
        private Dictionary<ulong, string> callNameList = new Dictionary<ulong, string>();
        //じゃんけんランキングリスト
        private Dictionary<ulong, JankenRankData> jankenRankList = new Dictionary<ulong, JankenRankData>();

        //一発育成杯計算用
        private IppatsuIkuseiHai ippatsuIkuseiHai = new IppatsuIkuseiHai();

        private PrivateFontCollection pfc = new PrivateFontCollection();

        private Font font18;
        private Font font30;
        private Font font50;
        private Font font70;

        private string Token => _configuration.GetValue<string>("Token");
        public DiscordBotService(IConfiguration configuration)
        {
            _configuration = configuration;

            //呼び名読み込み
            LoadCallName();

            //じゃんけんランク読み込み
            LoadJankenRank();

            //一発育成杯リスト
            ippatsuIkuseiHai.nameList = new List<string>();
            ippatsuIkuseiHai.scoreList = new List<int>();

            //フォント読み込み
            pfc.AddFontFile("Resources/font.ttf");

            font18 = new Font(pfc.Families[0], 18);
            font30 = new Font(pfc.Families[0], 30);
            font50 = new Font(pfc.Families[0], 50);
            font70 = new Font(pfc.Families[0], 70);
        }

        ~DiscordBotService()
        {
            font18.Dispose();
            font30.Dispose();
            font50.Dispose();
            font70.Dispose();
            pfc.Dispose();
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _client = new DiscordSocketClient(new DiscordSocketConfig { LogLevel = Discord.LogSeverity.Info });
            _client.Log += x =>
            {
                Console.WriteLine($"{x.Message}, {x.Exception}");
                return Task.CompletedTask;
            };
            _client.MessageReceived += onMessage;
            _client.Ready += onReady;
            _client.JoinedGuild += JoinedGuild;
            _client.UserJoined += UserJoined;
            await _client.LoginAsync(Discord.TokenType.Bot, Token);
            await _client.StartAsync();
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            await _client.StopAsync();
        }

        private Task UserJoined(SocketGuildUser user)
        {
            _client.GetGuild(serverID).GetTextChannel(842810363304869912).SendMessageAsync(
                user.Mention + "こんにちはっ！眠りの森へようこそ！\n" +
                "私はスマートファルコン！ファル子って呼んでね☆\n\n" +
                "ファル子、いろ～んなことができるのっ！ぜひファル子をメンションして、\n" +
                "```@スマートファルコン 使い方```\n" +
                "って送ってみてね！");
            return Task.CompletedTask;
        }

        private Task onReady()
        {
            Console.WriteLine($"{_client.CurrentUser} is Running!!");
            return Task.CompletedTask;
        }

        //参加時テキスト
        private async Task JoinedGuild(SocketGuild socketGuild)
        {
            await socketGuild.DefaultChannel.SendMessageAsync("こんにちは！スマートファルコンです☆\nまだまだできることは少ないけど、トレーナーのみなさんを元気にできるように頑張ります！よろしくね～！");
        }

        private async Task onMessage(SocketMessage message)
        {
            //自分自身だったらリターン
            if (message.Author.Id == _client.CurrentUser.Id)
            {
                return;
            }
            //メッセージが空だったらリターン
            if (message == null)
            {
                return;
            }
            //ボットからのメッセージだったらリターン
            if (message.Author.IsBot)
            {
                return;
            }

            SocketUserMessage msg = message as SocketUserMessage;
            CommandContext context = new CommandContext(_client, msg);

            //自分へのメンションか
            bool isMention = message.Content.Contains(_client.CurrentUser.Id.ToString()) || message.Content.Contains(otherID.ToString()) ||
                message.CleanContent.Contains("@スマートファルコン");

            //呼び名
            string authorName = message.Author.Username + "さん";
            if (callNameList.ContainsKey(message.Author.Id))
            {
                authorName = callNameList[message.Author.Id];
            }

            if (isMention)
            {
                //#if DEBUG
                if (message.Content.Contains("応答せよ"))
                {
                    await message.Channel.SendMessageAsync("私だ。");
                }
                //#endif


                if (message.Content.Contains("こんにちは"))
                {
                    await message.Channel.SendMessageAsync("こんにちは、" + authorName + "！");
                }
                else if (message.Content.Contains("おはよ"))
                {
                    await message.Channel.SendMessageAsync("おはよう、" + authorName + "！");
                }
                else if (message.Content.Contains("こんばんは"))
                {
                    await message.Channel.SendMessageAsync("こんばんは、" + authorName + "！");
                }
                else if (message.Content.Contains("おやすみ"))
                {
                    await message.Channel.SendMessageAsync("おやすみ、" + authorName + "～...");
                }
                else if (message.Content.Contains("ファ・ル・子"))
                {
                    await message.Channel.SendMessageAsync(authorName + "！応援ありがと～～～！！♡");
                }
                else if (message.Content.Contains("静かに"))
                {
                    await message.Channel.SendMessageAsync("はーい、しばらく静かにしてます...");

                    isSilent = true;
                }
                else if (message.Content.Contains("もういいよ"))
                {
                    await message.Channel.SendMessageAsync("はい！ファル子、みんなの会話聞いちゃいます☆");

                    isSilent = false;
                }
                else if (message.Content.Contains("好き"))
                {
                    Random rand = new Random();
                    int num = rand.Next(0, 3);

                    if (num == 0)
                    {
                        await message.Channel.SendMessageAsync(authorName + "...えへへ、なんだか照れちゃうな...♡");
                    }
                    else if (num == 1)
                    {
                        await message.Channel.SendMessageAsync(authorName + "...！ありがとう！！♡");
                    }
                    else if (num == 2)
                    {
                        await message.Channel.SendMessageAsync(authorName + "もファル子のかわいさがだんだんわかってきたね～♡");
                    }
                }
                else if (message.Content.Contains("かわいい") || message.Content.Contains("可愛い"))
                {
                    Random rand = new Random();
                    int num = rand.Next(0, 3);

                    if (num == 0)
                    {
                        await message.Channel.SendMessageAsync("えへへ～～、ありがとっ！" + authorName + "！♡");
                    }
                    else if (num == 1)
                    {
                        await message.Channel.SendMessageAsync("ファル子のかわいさ、もっと伝えちゃうぞ～☆");
                    }
                    else if (num == 2)
                    {
                        await message.Channel.SendMessageAsync("もう～～、褒めすぎだよ～～っ♡");
                    }
                }
                else if (message.Content.Contains("って呼んで"))
                {
                    //って呼んで　までの文字列抜き出し
                    int start = _client.CurrentUser.Mention.Length;
                    string name = message.Content.Substring(start, message.Content.IndexOf("って呼んで") - start);

                    //先頭が半角スペースだったら削除
                    if (name.StartsWith(" "))
                    {
                        name = name.Substring(1);
                    }

                    //既に存在していたら書き換え
                    if (callNameList.ContainsKey(message.Author.Id))
                    {
                        callNameList[message.Author.Id] = name;
                    }
                    //なければ追加
                    else
                    {
                        callNameList.Add(message.Author.Id, name);
                    }

                    //テキストファイルに上書き保存
                    SaveCallName();

                    //読み直し
                    LoadCallName();

                    await message.Channel.SendMessageAsync("は～い、これからは" + callNameList[message.Author.Id] + "って呼ぶね！");
                }
                else if (message.Content.Contains("しゃい☆"))
                {
                    if (message.Content.Contains("しゃいしゃい☆"))
                    {
                        await message.Channel.SendMessageAsync("っしゃいしゃいしゃ～いっ☆");
                    }
                    else
                    {
                        Random rand = new Random();
                        int num = rand.Next(0, 2);

                        if (num == 0)
                        {
                            await message.Channel.SendMessageAsync("う～～～～...............っしゃい！！");
                        }
                        else
                        {
                            await message.Channel.SendMessageAsync("っしゃいしゃ～いっ☆");
                        }
                    }
                }
                //else if (message.Content.Contains("占って"))
                //{
                //    //20220101のようなシード値
                //    int seedDay = DateTime.Today.Year * 10000 + DateTime.Today.Month * 1000 + DateTime.Today.Day;
                //    //メッセージの送り主によって決まる値
                //    int seedID = (int)message.Author.Id % 9999;
                //    //2つを足した値をシード値にする
                //    int seed = seedDay + seedID;

                //    //シード値から乱数生成 (日替わりで違う結果になる)
                //    Random rand = new Random(seed);
                //    int num = rand.Next(0, 100);

                //    //送る文章
                //    string send = authorName + "の今日の運勢は...";

                //    //大大吉 (5/100)
                //    if (num < 5)
                //    {
                //        send += "**大大吉**！！！\nすご～～い！おめでとう！！今日はとってもいいことがあるかも！！";
                //    }
                //    //大吉 (15/100)
                //    else if (num < 20)
                //    {
                //        send += "**大吉**！\nやった～！今日の運勢はバッチリ！";
                //    }
                //    //中吉 (20/100)
                //    else if (num < 40)
                //    {
                //        send += "**中吉**！\n今日も元気にがんばろう～！";
                //    }
                //    //吉 (25/100)
                //    else if (num < 65)
                //    {
                //        send += "**吉**！\n今日も無事に一日を過ごせますように！";
                //    }
                //    //小吉 (20/100)
                //    else if (num < 85)
                //    {
                //        send += "**小吉**！\n小さな幸せ、あるかも？";
                //    }
                //    //凶 (10 /100)
                //    else if (num < 95)
                //    {
                //        send += "**凶**...\nお、落ち込まないで！いいことあるよ...！きっと！";
                //    }
                //    //大凶 (4 /100)
                //    else if (num < 98)
                //    {
                //        send += "**大凶**......\n今日は身の周りに注意かも...";
                //    }
                //    //超大吉 (1/100)
                //    else if (num < 99)
                //    {
                //        send += "**超大吉**！？！？\nえ～～～っ！！なにこれ！！すごすぎるよ～～！！！\n今日ガシャを引いたらもしかしちゃうかも！？！？";
                //    }
                //    //死 (1/100)
                //    else if (num < 100)
                //    {
                //        send += "**......**\nちょ、ちょっとファル子の口からは言えないかなあ......あはははは...\n今日はおうちから出ないほうがいいかもね...？";
                //    }

                //    //ラッキーキャラ
                //    num = rand.Next(0, 88);

                //    send += "\n\nラッキーキャラ：" + GetRndUmaName(num);

                //    //ラッキー適正
                //    //バ場
                //    num = rand.Next(0, 5);

                //    send += "\nラッキー適正：" + GetRndFieldName(num);

                //    //距離
                //    num = rand.Next(0, 4);

                //    send += " " + GetRndDistanceName(num, GetRndFieldName(num));

                //    //脚質
                //    num = rand.Next(0, 4);

                //    send += " " + GetRndLegName(num);

                //    //送信
                //    await message.Channel.SendMessageAsync(send);
                //}
                else if (message.Content.Contains("占って"))
                {
                    //20220101のようなシード値
                    int seedDay = DateTime.Today.Year * 10000 + DateTime.Today.Month * 1000 + DateTime.Today.Day;
                    //メッセージの送り主によって決まる値
                    int seedID = (int)message.Author.Id % 9999;
                    //2つを足した値をシード値にする
                    int seed = seedDay + seedID;


                    //画像生成
                    string path = "Resources/tmp.png";
                    CreateFortuneImg(authorName, seed, path);

                    //送信
                    await message.Channel.SendFileAsync(path);

                    //削除
                    File.Delete(path);
                }
                else if (message.Content.Contains("慰めて"))
                {
                    //送信
                    await message.Channel.SendMessageAsync("よしよし...つらかったね...どんなにつらくてもファル子がいるからね...！");
                }
                else if (message.Content.Contains("じゃんけん"))
                {
                    //ランキング取得
                    if (message.Content.Contains("ランキング"))
                    {
                        //ランキング取得
                        string output = GetRankStr();

                        await message.Channel.SendMessageAsync(output);
                    }
                    else
                    {
                        //ランダムで手を決める
                        Random rand = new Random();
                        int num = rand.Next(0, 3);
                        int result = -1;

                        string falcoHand = "";

                        //グー
                        if (num == 0)
                        {
                            if (message.Content.Contains("グー") || message.Content.Contains("ぐー") || message.Content.Contains(":fist:") || message.Content.Contains("✊"))
                            {
                                result = 2;
                            }
                            else if (message.Content.Contains("チョキ") || message.Content.Contains("ちょき") || message.Content.Contains(":v:") || message.Content.Contains("✌"))
                            {
                                result = 1;
                            }
                            else if (message.Content.Contains("パー") || message.Content.Contains("ぱー") || message.Content.Contains(":hand_splayed:") || message.Content.Contains("✋"))
                            {
                                result = 0;
                            }

                            falcoHand = ":fist:";
                        }
                        //チョキ
                        else if (num == 1)
                        {
                            if (message.Content.Contains("グー") || message.Content.Contains("ぐー") || message.Content.Contains(":fist:") || message.Content.Contains("✊"))
                            {
                                result = 0;
                            }
                            else if (message.Content.Contains("チョキ") || message.Content.Contains("ちょき") || message.Content.Contains(":v:") || message.Content.Contains("✌"))
                            {
                                result = 2;
                            }
                            else if (message.Content.Contains("パー") || message.Content.Contains("ぱー") || message.Content.Contains(":hand_splayed:") || message.Content.Contains("✋"))
                            {
                                result = 1;
                            }

                            falcoHand = ":v:";
                        }
                        //パー
                        else if (num == 2)
                        {
                            if (message.Content.Contains("グー") || message.Content.Contains("ぐー") || message.Content.Contains(":fist:") || message.Content.Contains("✊"))
                            {
                                result = 1;
                            }
                            else if (message.Content.Contains("チョキ") || message.Content.Contains("ちょき") || message.Content.Contains(":v:") || message.Content.Contains("✌"))
                            {
                                result = 0;
                            }
                            else if (message.Content.Contains("パー") || message.Content.Contains("ぱー") || message.Content.Contains(":hand_splayed:") || message.Content.Contains("✋"))
                            {
                                result = 2;
                            }

                            falcoHand = ":hand_splayed:";
                        }

                        //手を指定してなかったら指定するようにいう
                        if (result == -1)
                        {
                            await message.Channel.SendMessageAsync("じゃんけんの手を指定してね！\n例:じゃんけん グー");
                        }
                        else if (result == 0)
                        {
                            await message.Channel.SendMessageAsync(falcoHand + "\nファル子の負け～～...\n悔しい～～！ 次は勝つからね！！");


                            //既に存在していたら書き換え
                            if (jankenRankList.ContainsKey(message.Author.Id))
                            {
                                //ポイント加算
                                jankenRankList[message.Author.Id].point += 10;
                            }
                            //なければ追加
                            else
                            {
                                JankenRankData data = new JankenRankData();
                                data.name = message.Author.Username;
                                data.point = 10;
                                jankenRankList.Add(message.Author.Id, data);
                            }

                            SaveJankenRank();

                            LoadJankenRank();

                            //現在のランク取得
                            int nowRank = GetJankenRanking(message.Author.Id);

                            await message.Channel.SendMessageAsync("現在のポイント:" + jankenRankList[message.Author.Id].point + "pt, " + nowRank + "位");
                        }
                        else if (result == 1)
                        {
                            await message.Channel.SendMessageAsync(falcoHand + "\nファル子の勝ち～！！ やった～～～☆");

                            //既に存在していたら書き換え
                            if (jankenRankList.ContainsKey(message.Author.Id))
                            {
                                //ポイント減算
                                jankenRankList[message.Author.Id].point -= 5;
                            }
                            //なければ追加
                            else
                            {
                                JankenRankData data = new JankenRankData();
                                data.name = message.Author.Username;
                                data.point = -5;
                                jankenRankList.Add(message.Author.Id, data);
                            }

                            SaveJankenRank();

                            LoadJankenRank();

                            //現在のランク取得
                            int nowRank = GetJankenRanking(message.Author.Id);

                            await message.Channel.SendMessageAsync("現在のポイント:" + jankenRankList[message.Author.Id].point + "pt, " + nowRank + "位");
                        }
                        else if (result == 2)
                        {
                            await message.Channel.SendMessageAsync(falcoHand + "\nあいこ！もう一回じゃんけんしよ！");
                        }
                    }
                }
                else if (message.Content.Contains("IIH"))
                {
                    //一発育成杯を開始
                    if (message.Content.Contains("start"))
                    {
                        bool isThrowException = false;
                        bool isAddSameName = false;

                        string output = "一発育成杯を始めるよ～！\n参加者:";

                        if (ippatsuIkuseiHai.isStart == true)
                        {
                            output = "開催中の大会を中断して、新しく" + output;
                        }

                        //一応リセット
                        ResetIppatsuIkuseiHai();

                        //スタート
                        ippatsuIkuseiHai.isStart = true;


                        //参加者登録
                        //IIH start 名前1 名前2 名前3...という形式
                        try
                        {
                            string strInput = message.Content.Substring(message.Content.IndexOf("start") + 6);

                            while (strInput.Length != 0)
                            {
                                string name = "";

                                int length = strInput.IndexOf(" ");

                                //最後の名前
                                if (length == -1)
                                {
                                    name = strInput;
                                }
                                else
                                {
                                    name = strInput.Substring(0, length);
                                }

                                //同じ名前が追加されてないかチェック
                                if (ippatsuIkuseiHai.nameList.Contains(name) == true)
                                {
                                    isAddSameName = true;
                                    break;
                                }

                                //名前と初期スコアを追加
                                ippatsuIkuseiHai.nameList.Add(name);
                                ippatsuIkuseiHai.scoreList.Add(0);

                                if (length == -1)
                                {
                                    break;
                                }

                                strInput = strInput.Substring(length + 1);
                            }
                        }
                        catch (Exception e)
                        {
                            isThrowException = true;
                        }

                        //同名の参加者が追加されたとき
                        if (isAddSameName == true)
                        {
                            await message.Channel.SendMessageAsync("参加者の名前は他と被らないように設定してほしいな...！");
                            ResetIppatsuIkuseiHai();
                        }
                        //参加者は...誰一人...来ませんでした...の時と例外が投げられたときの処理
                        else if (ippatsuIkuseiHai.nameList.Count == 0 || isThrowException == true)
                        {
                            await message.Channel.SendMessageAsync(
                                "うーん...参加者をうまく読み取れなかったから、参加者名を正しく入力して欲しいな。\n" +
                                "例:「IIH start 名前1 名前2 名前3」");

                            ResetIppatsuIkuseiHai();
                        }
                        //参加者1人のとき
                        else if (ippatsuIkuseiHai.nameList.Count == 1)
                        {
                            await message.Channel.SendMessageAsync("参加者が1人じゃ大会にならないよ～:sweat_drops:");
                            ResetIppatsuIkuseiHai();
                        }
                        else
                        {
                            foreach (var v in ippatsuIkuseiHai.nameList)
                            {
                                output += v + "、";
                            }

                            //最後の「、」を取り除く
                            output = output.Substring(0, output.Length - 1);

                            await message.Channel.SendMessageAsync(output);
                        }

                    }
                    //一発育成杯を終了(結果表示)
                    else if (message.Content.Contains("end"))
                    {
                        if (ippatsuIkuseiHai.isStart == false)
                        {
                            await message.Channel.SendMessageAsync("まだ一発育成杯が開催されてないみたい...");
                        }
                        else
                        {
                            string output = "結果は......！\n";
                            List<string> winnerList = new List<string>();
                            List<int> indices = new List<int>();
                            int maxScore = 0;

                            //ポイントが高い順にソート
                            for (int i = 0; i < ippatsuIkuseiHai.nameList.Count; i++)
                            {
                                indices.Add(i);
                                for (int j = indices.Count - 1; j > 0; j--)
                                {
                                    //登録しようとしてるスコアが元々あったスコアより高かったら入れ替え
                                    if (ippatsuIkuseiHai.scoreList[indices[j]] > ippatsuIkuseiHai.scoreList[indices[j - 1]])
                                    {
                                        int tmp = indices[j - 1];
                                        indices[j - 1] = indices[j];
                                        indices[j] = tmp;
                                    }
                                }
                            }

                            //内訳出力
                            for (int i = 0; i < ippatsuIkuseiHai.nameList.Count; i++)
                            {
                                output += ippatsuIkuseiHai.nameList[indices[i]] + "\t" + ippatsuIkuseiHai.scoreList[indices[i]] + "pt\n";

                                //最高得点を記録
                                if (ippatsuIkuseiHai.scoreList[indices[i]] > maxScore)
                                {
                                    maxScore = ippatsuIkuseiHai.scoreList[indices[i]];
                                }
                            }

                            //優勝者をリストに入れる
                            for (int i = 0; i < ippatsuIkuseiHai.nameList.Count; i++)
                            {
                                if (ippatsuIkuseiHai.scoreList[indices[i]] == maxScore)
                                {
                                    winnerList.Add(ippatsuIkuseiHai.nameList[indices[i]]);
                                }
                            }

                            output += "\n優勝者は......";

                            //優勝者出力
                            foreach (var v in winnerList)
                            {
                                output += "「" + v + "」、";
                            }

                            //最後の「、」を取り除く
                            output = output.Substring(0, output.Length - 1);

                            output += "！！！\n優勝おめでと～～～！！！☆☆";

                            await message.Channel.SendMessageAsync(output);

                            //一応リセット
                            ResetIppatsuIkuseiHai();

                        }
                    }
                    //開催中の一発育成杯をキャンセルする
                    else if (message.Content.Contains("cancel"))
                    {
                        if (ippatsuIkuseiHai.isStart == false)
                        {
                            await message.Channel.SendMessageAsync("一発育成杯が開催されていなかったみたい！");
                        }
                        else
                        {
                            //リセット
                            ResetIppatsuIkuseiHai();

                            await message.Channel.SendMessageAsync("開催中だった一発育成杯を中止したよ！");
                        }
                    }
                    else if (message.Content.Contains("save"))
                    {
                        //参加者数
                        int numParticipant = ippatsuIkuseiHai.nameList.Count;
                        //登録するスコアリスト
                        List<int> saveScoreList = new List<int>();
                        //参加者数だけ要素追加
                        for (int i = 0; i < numParticipant; i++) { saveScoreList.Add(0); }
                        //例外処理が投げられたか
                        bool isThrowException = false;
                        //名前が登録されていないものだったとき
                        bool notFoundName = false;
                        //同じ名前が2度追加されたとき
                        bool isAddSameName = false;
                        //得点
                        int score = numParticipant;

                        //処理に使用する文字列
                        try
                        {
                            string strInput = message.Content.Substring(message.Content.IndexOf("save") + 5);

                            while (strInput.Length != 0)
                            {
                                string name = "";

                                int length = strInput.IndexOf(" ");

                                //最後の名前
                                if (length == -1)
                                {
                                    name = strInput;
                                }
                                else
                                {
                                    name = strInput.Substring(0, length);
                                }

                                //名前からインデックス取得
                                int index = ippatsuIkuseiHai.nameList.IndexOf(name);

                                //登録されていない名前であったら
                                if (index == -1)
                                {
                                    notFoundName = true;
                                    break;
                                }
                                //同じ名前が2度登録されたら
                                else if (saveScoreList[index] != 0)
                                {
                                    isAddSameName = true;
                                    break;
                                }

                                saveScoreList[index] = score;

                                score--;

                                strInput = strInput.Substring(length + 1);

                                if (length == -1)
                                {
                                    break;
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            isThrowException = true;
                        }

                        //例外が投げられたら
                        if (isThrowException == true)
                        {
                            await message.Channel.SendMessageAsync(
                                "うーん...うまくスコアを読み取れなかったから、1位から順に正しくスコアを入力してほしいな。\n" +
                                "例:「IIH save 名前1 名前2 名前3」"
                                );
                        }
                        else if (notFoundName == true)
                        {
                            await message.Channel.SendMessageAsync("登録されていない名前が入力されたみたい！もう一度正しく入力してほしいな...！");
                        }
                        else if (isAddSameName == true)
                        {
                            await message.Channel.SendMessageAsync("同じ名前が複数登録されているみたい！もう一度正しく入力してほしいな...！");
                        }
                        //入力されたスコア数が参加人数より少なかった時
                        else if (saveScoreList.Count < numParticipant)
                        {
                            await message.Channel.SendMessageAsync("スコアの数が参加者より少ないみたい...\nもう一度入力して欲しいな。");
                        }
                        //入力されたスコア数が参加人数より多かった時
                        else if (saveScoreList.Count > numParticipant)
                        {
                            await message.Channel.SendMessageAsync("スコアの数が参加者より多いみたい...\nもう一度入力して欲しいな。");
                        }
                        else
                        {
                            //問題なければスコア登録
                            for (int i = 0; i < saveScoreList.Count; i++)
                            {
                                ippatsuIkuseiHai.scoreList[i] += saveScoreList[i];
                            }

                            string output = ippatsuIkuseiHai.raceCount + "回戦目のスコアを登録したよ！\n";
                            ippatsuIkuseiHai.raceCount++;

                            //ランダム出力
                            Random rand = new Random();
                            int num = rand.Next(3);

                            if (num == 0)
                            {
                                output += "みんな～！頑張ってね～～！！";
                            }
                            else if (num == 1)
                            {
                                output += "誰が優勝するのかな～、ファル子、ワクワクしてきちゃった！";
                            }
                            else if (num == 2)
                            {
                                output += "みんな、諦めないで頑張ろう～～！！";
                            }

                            await message.Channel.SendMessageAsync(output);
                        }
                    }
                    else if (message.Content.Contains("trophy"))
                    {
                        string output = "---サークル内一発育成杯 トロフィー一覧---\n";

                        output += "第零回\tエルコンドルパサー\tマイル\t椎名\n";
                        output += "第一回\tマヤノトップガン(花嫁)\t中距離\tりゅう\n";
                        output += "第二回\tウオッカ\tマイル\tコーシー\n";
                        output += "第三回\tメジロマックイーン\t長距離\tりゅう\n";
                        output += "第四回\tハルウララ\tダート\tりゅう\n";
                        output += "第五回\tキングヘイロー\t短距離\tクマ\n";
                        output += "第六回\tスペシャルウィーク(水着)\t中距離\tクマ\n";
                        output += "第七回\t無料単発で出たキャラ\tマイル\t椎名(マチカネフクキタル)\n";
                        output += "第八回\tゴールドシップ\t長距離\tクマ\n";
                        output += "第九回\tナイスネイチャ\t中距離\tりゅう\tクマ\n";

                        await message.Channel.SendMessageAsync(output);
                    }
                    else if (message.Content.Contains("rule"))
                    {
                        await message.Channel.SendMessageAsync("一発育成杯のルールはこの投稿を見てね～☆");
                        await message.Channel.SendMessageAsync("https://discord.com/channels/842810363304869909/950089196176019486/950090352101060708");
                    }
                }
                else if (message.Content.Contains("因子シミュ"))
                {
                    int count = 1;
                    if (message.Content.Contains("因子シミュ5連"))
                    {
                        count = 5;
                    }

                    try
                    {
                        string[] str = message.CleanContent.Split(' ');

                        //画像生成
                        string path = "Resources/tmp2.png";

                        for (int i = 0; i < count; i++)
                        {
                            //[0][1]はヘッダなので[2]から
                            if (str.Length < 8)
                            {
                                CreateInshiImg(int.Parse(str[2]), int.Parse(str[3]), int.Parse(str[4]), int.Parse(str[5]), int.Parse(str[6]), "", path);
                            }
                            else
                            {
                                CreateInshiImg(int.Parse(str[2]), int.Parse(str[3]), int.Parse(str[4]), int.Parse(str[5]), int.Parse(str[6]), str[7], path);
                            }

                            //送信
                            await message.Channel.SendFileAsync(path);

                            //削除
                            File.Delete(path);
                        }
                    }
                    catch (Exception e)
                    {
                        //送信
                        await message.Channel.SendMessageAsync("エラーが発生したみたい...\nコマンド構文におかしいところがないかチェックしてみてね！");
                    }
                }
                else if (message.Content.Contains("誕生日") && message.Content.Contains("おめ"))
                {
                    //送信
                    await message.Channel.SendMessageAsync("わぁ......！！！ありがとう～～～！！！☆\n今年もファル子のかわいさ、" + authorName + "に伝えちゃうよ～～♡");
                }
                else if (message.Content.Contains("すけべしようや"))
                {
                    //18↑ロールを付与
                    var role = context.Guild.Roles.FirstOrDefault(x => x.Name == "18↑");
                    await (context.User as IGuildUser).AddRoleAsync(role);

                    //送信
                    await message.Channel.SendMessageAsync("も～～っ！そういうのはファル子的にOUT！！！\n※🔞チャンネルが解禁されました。やったね！");
                }
                else if (message.Content.Contains("先月") && message.Content.Contains("ファン数"))
                {
                    //Excelシート読み込み
                    IWorkbook workbook = WorkbookFactory.Create("Resources/Circle_Fans.xlsx");
                    ISheet worksheet = workbook.GetSheetAt(1);

                    //月取得
                    IRow row = worksheet.GetRow(0);
                    ICell cell = row.GetCell(6);
                    string month = cell.NumericCellValue.ToString();

                    //0行目にIDが書いてある
                    for (int i = 0; i < 30; i++)
                    {
                        row = worksheet.GetRow(i);
                        cell = row.GetCell(0);
                        if (message.Author.Id.ToString() == cell.StringCellValue)
                        {
                            cell = row.GetCell(3);

                            string transition = cell.NumericCellValue.ToString();

                            //差分
                            if (message.Content.Contains("差分"))
                            {
                                try
                                {
                                    //ファン数読み取り
                                    int start = message.Content.LastIndexOf(" ") + 1;
                                    long fan = long.Parse(message.Content.Substring(start));
                                    long oldFan = (long)row.GetCell(2).NumericCellValue;
                                    long sub = fan - oldFan;
                                    if (sub < 0)
                                    {
                                        //送信
                                        await message.Channel.SendMessageAsync("ファン数指定には今現在のファン数を指定してね！");
                                    }
                                    else
                                    {
                                        //送信
                                        await message.Channel.SendMessageAsync(month + "月末から現在までの獲得ファン数は、" + sub + "人だよ！");
                                    }
                                }
                                catch (Exception e)
                                {
                                    //送信
                                    await message.Channel.SendMessageAsync("ファン数をうまく読み込めなかったみたい...もう一度正しく入力してみてね！\n(例)「先月のファン数からの差分を教えて 123456789」");
                                }
                            }
                            else
                            {
                                //送信
                                await message.Channel.SendMessageAsync(month + "月の" + authorName + "の月間ファン数推移は、" + transition + "人だよ！");
                            }

                            return;
                        }
                    }

                    //送信
                    await message.Channel.SendMessageAsync("ごめ～ん..." + authorName + "のDiscordのIDをコーシーに報告してもらってもいいかな？未登録みたい...");
                }
                else if (message.Content.Contains("使い方") || message.Content.Contains("つかいかた"))
                {
                    //送信
                    await message.Channel.SendMessageAsync("使い方はここを見てね！\nhttps://github.com/taikousi504/SmartFalconBot/blob/master/README.md");
                }
            }
            else
            {
                //静かにしてるよう言われてたら発言を拾わない
                if (isSilent)
                {
                    return;
                }

                if (message.Content.Contains("アイドル"))
                {
                    Random rand = new Random();
                    int num = rand.Next(0, 3);

                    if (num == 0)
                    {
                        await message.Channel.SendMessageAsync("ウマドルですっ");
                    }
                    else if (num == 1)
                    {
                        await message.Channel.SendMessageAsync("ウマドルだよっ");
                    }
                    else if (num == 2)
                    {
                        await message.Channel.SendMessageAsync("ウマドルのことかな？");
                    }

                }
                else if (message.Content.Contains("ファル子"))
                {
                    Random rand = new Random();
                    int num = rand.Next(0, 4);

                    if (num == 0)
                    {
                        await message.Channel.SendMessageAsync("ファル子で～す☆");
                    }
                    else if (num == 1)
                    {
                        await message.Channel.SendMessageAsync("呼んだ～？☆");
                    }
                    else if (num == 2)
                    {
                        await message.Channel.SendMessageAsync("はい！ファル子、頑張っちゃう♡");
                    }
                    else if (num == 3)
                    {
                        await message.Channel.SendMessageAsync("ちらっ...:eyes:");
                    }
                }


                //ランダムでリアクションを付ける
                Random randReact = new Random();
                int numReact = randReact.Next(0, 50);

                //1/50の確率で
                if (numReact == 0)
                {
                    var emote = Emote.Parse("<:emoji_7:939189407016177684>");

                    await message.AddReactionAsync(emote);
                }

            }
        }

        //呼び名ロード
        private void LoadCallName()
        {
            //まずリストを空に。
            callNameList.Clear();

            //ファイルがなかったらスルー
            if (!File.Exists("Resources/CallNameList.txt"))
            {
                return;
            }

            //ファイル読み込み
            StreamReader sr = new StreamReader("Resources/CallNameList.txt");

            while (!sr.EndOfStream)
            {
                string line = sr.ReadLine();
                string[] str = line.Split(',');

                //0番目にID、1番目に呼び名が入る
                ulong id;
                try
                {
                    id = ulong.Parse(str[0]);
                }
                catch (Exception e)
                {
                    id = 0;
                }
                string callName = str[1];

                //追加
                if (id != 0 && string.IsNullOrEmpty(callName) == false)
                {
                    callNameList.Add(id, callName);
                }
            }

            sr.Close();
            sr.Dispose();
        }

        //呼び名セーブ
        private void SaveCallName()
        {
            StreamWriter sr = new StreamWriter("Resources/CallNameList.txt");
            foreach (var v in callNameList)
            {
                sr.WriteLine(v.Key + "," + v.Value);
            }
            sr.Close();
            sr.Dispose();
        }

        //じゃんけんランキングロード
        private void LoadJankenRank()
        {
            //まずリストを空に。
            jankenRankList.Clear();

            //ファイルがなかったらスルー
            if (!File.Exists("Resources/JankenRankList.txt"))
            {
                return;
            }

            //ファイル読み込み
            StreamReader sr = new StreamReader("Resources/JankenRankList.txt");

            while (!sr.EndOfStream)
            {
                string line = sr.ReadLine();
                string[] str = line.Split(',');

                //0番目にID、1番目に呼び名が入る
                ulong id;
                try
                {
                    id = ulong.Parse(str[0]);
                }
                catch (Exception e)
                {
                    id = 0;
                }

                string name = str[1];
                int point = int.Parse(str[2]);

                //追加
                if (id != 0)
                {
                    JankenRankData data = new JankenRankData();
                    data.name = name;
                    data.point = point;
                    jankenRankList.Add(id, data);
                }
            }

            sr.Close();
            sr.Dispose();
        }

        //じゃんけんランキングセーブ
        private void SaveJankenRank()
        {
            StreamWriter sr = new StreamWriter("Resources/JankenRankList.txt");
            foreach (var v in jankenRankList)
            {
                sr.WriteLine(v.Key + "," + v.Value.name + "," + v.Value.point);
            }
            sr.Close();
            sr.Dispose();
        }

        //現在の順位取得
        private int GetJankenRanking(ulong id)
        {
            //ランキングに登録されていなかったら-1を返す
            if (jankenRankList.ContainsKey(id) == false)
            {
                return -1;
            }

            // ソート
            var sorted = jankenRankList.OrderByDescending(pair => pair.Value.point);
            var list = sorted.ToDictionary(x => x.Key);

            int count = 1;
            foreach (var v in list)
            {
                if (v.Key == id)
                {
                    return count;
                }

                count++;
            }

            return -1;
        }

        private string GetRankStr()
        {
            string result = "---ファル子じゃんけんランキング---\n";

            // ソート
            var sorted = jankenRankList.OrderByDescending(pair => pair.Value.point);
            var list = sorted.ToDictionary(x => x.Key);

            int count = 1;
            foreach (var v in list)
            {
                result += count + "位:" + v.Value.Value.name + "\t" + v.Value.Value.point + "pt\n";

                count++;
            }

            return result;
        }

        private string GetRndUmaName(int num)
        {
            if (num == 0)
            {
                return "スペシャルウィーク";
            }
            else if (num == 1)
            {
                return "サイレンススズカ";
            }
            else if (num == 2)
            {
                return "トウカイテイオー";
            }
            else if (num == 3)
            {
                return "マルゼンスキー";
            }
            else if (num == 4)
            {
                return "フジキセキ";
            }
            else if (num == 5)
            {
                return "オグリキャップ";
            }
            else if (num == 6)
            {
                return "ゴールドシップ";
            }
            else if (num == 7)
            {
                return "ウオッカ";
            }
            else if (num == 8)
            {
                return "ダイワスカーレット";
            }
            else if (num == 9)
            {
                return "タイキシャトル";
            }
            else if (num == 10)
            {
                return "グラスワンダー";
            }
            else if (num == 11)
            {
                return "ヒシアマゾン";
            }
            else if (num == 12)
            {
                return "メジロマックイーン";
            }
            else if (num == 13)
            {
                return "エルコンドルパサー";
            }
            else if (num == 14)
            {
                return "テイエムオペラオー";
            }
            else if (num == 15)
            {
                return "ナリタブライアン";
            }
            else if (num == 16)
            {
                return "シンボリルドルフ";
            }
            else if (num == 17)
            {
                return "エアグルーヴ";
            }
            else if (num == 18)
            {
                return "アグネスデジタル";
            }
            else if (num == 19)
            {
                return "タマモクロス";
            }
            else if (num == 20)
            {
                return "セイウンスカイ";
            }
            else if (num == 21)
            {
                return "ファインモーション";
            }
            else if (num == 22)
            {
                return "ビワハヤヒデ";
            }
            else if (num == 23)
            {
                return "マヤノトップガン";
            }
            else if (num == 24)
            {
                return "マンハッタンカフェ";
            }
            else if (num == 25)
            {
                return "ミホノブルボン";
            }
            else if (num == 26)
            {
                return "メジロライアン";
            }
            else if (num == 27)
            {
                return "ヒシアケボノ";
            }
            else if (num == 28)
            {
                return "ユキノビジン";
            }
            else if (num == 29)
            {
                return "ライスシャワー";
            }
            else if (num == 30)
            {
                return "アイネスフウジン";
            }
            else if (num == 31)
            {
                return "アグネスタキオン";
            }
            else if (num == 32)
            {
                return "アドマイヤベガ";
            }
            else if (num == 33)
            {
                return "イナリワン";
            }
            else if (num == 34)
            {
                return "ウイニングチケット";
            }
            else if (num == 35)
            {
                return "エアシャカール";
            }
            else if (num == 36)
            {
                return "カレンチャン";
            }
            else if (num == 37)
            {
                return "エイシンフラッシュ";
            }
            else if (num == 38)
            {
                return "カワカミプリンセス";
            }
            else if (num == 39)
            {
                return "ゴールドシチー";
            }
            else if (num == 40)
            {
                return "シーキングザパール";
            }
            else if (num == 41)
            {
                return "サクラバクシンオー";
            }
            else if (num == 42)
            {
                return "シンコウウインディ";
            }
            else if (num == 43)
            {
                return "スイープトウショウ";
            }
            else if (num == 44)
            {
                return "スーパークリーク";
            }
            else if (num == 45)
            {
                return "スマートファルコン";
            }
            else if (num == 46)
            {
                return "ゼンノロブロイ";
            }
            else if (num == 47)
            {
                return "トーセンジョーダン";
            }
            else if (num == 48)
            {
                return "ナカヤマフェスタ";
            }
            else if (num == 49)
            {
                return "ナリタタイシン";
            }
            else if (num == 50)
            {
                return "ニシノフラワー";
            }
            else if (num == 51)
            {
                return "ハルウララ";
            }
            else if (num == 52)
            {
                return "バンブーメモリー";
            }
            else if (num == 53)
            {
                return "マーベラスサンデー";
            }
            else if (num == 54)
            {
                return "ビコーペガサス";
            }
            else if (num == 55)
            {
                return "マチカネフクキタル";
            }
            else if (num == 56)
            {
                return "ミスターシービー";
            }
            else if (num == 57)
            {
                return "メイショウドトウ";
            }
            else if (num == 58)
            {
                return "メジロドーベル";
            }
            else if (num == 59)
            {
                return "ナイスネイチャ";
            }
            else if (num == 60)
            {
                return "キングヘイロー";
            }
            else if (num == 61)
            {
                return "マチカネタンホイザ";
            }
            else if (num == 62)
            {
                return "イクノディクタス";
            }
            else if (num == 63)
            {
                return "メジロパーマー";
            }
            else if (num == 64)
            {
                return "ダイタクヘリオス";
            }
            else if (num == 65)
            {
                return "ツインターボ";
            }
            else if (num == 66)
            {
                return "サトノダイヤモンド";
            }
            else if (num == 67)
            {
                return "キタサンブラック";
            }
            else if (num == 68)
            {
                return "サクラチヨノオー";
            }
            else if (num == 69)
            {
                return "シリウスシンボリ";
            }
            else if (num == 70)
            {
                return "メジロアルダン";
            }
            else if (num == 71)
            {
                return "ヤエノムテキ";
            }
            else if (num == 72)
            {
                return "メジロブライト";
            }
            else if (num == 73)
            {
                return "サクラローレル";
            }
            else if (num == 74)
            {
                return "ナリタトップロード";
            }
            else if (num == 75)
            {
                return "ヤマニンゼファー";
            }
            else if (num == 76)
            {
                return "アストンマーチャン";
            }
            else if (num == 77)
            {
                return "？？？(黒髪のほう)";
            }
            else if (num == 78)
            {
                return "？？？(帽子のほう)";
            }
            else if (num == 79)
            {
                return "ハッピーミーク";
            }
            else if (num == 80)
            {
                return "ビターグラッセ";
            }
            else if (num == 81)
            {
                return "リトルココン";
            }
            else if (num == 82)
            {
                return "駿川たづな";
            }
            else if (num == 83)
            {
                return "秋川やよい";
            }
            else if (num == 84)
            {
                return "乙名史悦子";
            }
            else if (num == 85)
            {
                return "桐生院葵";
            }
            else if (num == 86)
            {
                return "安心沢刺々美";
            }
            else if (num == 87)
            {
                return "樫本理子";
            }
            else
            {
                return "全員♡";
            }
        }

        private string GetRndFieldName(int num)
        {
            if (num < 4)
            {
                return "芝";
            }
            else
            {
                return "ダート";
            }
        }

        private string GetRndDistanceName(int num, string field)
        {
            if (num == 0)
            {
                return "短距離";
            }
            else if (num == 1)
            {
                return "マイル";
            }
            else if (num == 2)
            {
                return "中距離";
            }
            else
            {
                if (field == "芝")
                {
                    return "長距離";
                }
                else
                {
                    return "マイル";
                }
            }
        }

        private string GetRndLegName(int num)
        {
            if (num == 0)
            {
                return "逃げ";
            }
            else if (num == 1)
            {
                return "先行";
            }
            else if (num == 2)
            {
                return "差し";
            }
            else
            {
                return "追込";
            }
        }

        private void ResetIppatsuIkuseiHai()
        {
            ippatsuIkuseiHai.nameList.Clear();
            ippatsuIkuseiHai.scoreList.Clear();
            ippatsuIkuseiHai.raceCount = 1;
            ippatsuIkuseiHai.isStart = false;
        }
        private void CreateFortuneImg(string authorName, int seed, string dstPath)
        {
            #region 占い結果生成
            //シード値から乱数生成 (日替わりで違う結果になる)
            Random rand = new Random(seed);
            int num = rand.Next(0, 100);

            //送る文章
            string fortune,
                     comment,
                     chara,
                     field,
                     distance,
                     leg;

            //因子運
            int[] starCount = new int[6];
            int allStarCount = 0;
            for (int i = 0; i < starCount.Length; i++)
            {
                //確率で0個～3個に分類
                starCount[i] = rand.Next(0, 100);

                //0個 (15%)
                if (starCount[i] < 15)
                {
                    starCount[i] = 0;
                }
                //1個 (40%)
                else if (starCount[i] < 55)
                {
                    starCount[i] = 1;
                }
                //2個 (30%)
                else if (starCount[i] < 85)
                {
                    starCount[i] = 2;
                }
                //3個 (15%)
                else
                {
                    starCount[i] = 3;
                }

                allStarCount += starCount[i];
            }


            //超大吉 (1/100)
            if (allStarCount >= 16)
            {
                fortune = "超大吉";
                comment = "今日ガシャを引いたらもしかしちゃうかも！？！？";
            }
            //大大吉 (5/100)
            else if (allStarCount >= 14)
            {
                fortune = "大大吉";
                comment = "今日はとってもいいことがあるかも！！";
            }
            //大吉 (15/100)
            else if (allStarCount >= 12)
            {
                fortune = "大吉";
                comment = "やった～！今日の運勢はバッチリ！";
            }
            //中吉 (20/100)
            else if (allStarCount >= 10)
            {
                fortune = "中吉";
                comment = "今日も元気にがんばろう～！";
            }
            //吉 (25/100)
            else if (allStarCount >= 8)
            {
                fortune = "吉";
                comment = "今日も無事に一日を過ごせますように！";
            }
            //小吉 (20/100)
            else if (allStarCount >= 6)
            {
                fortune = "小吉";
                comment = "小さな幸せ、あるかも？";
            }
            //凶 (10 /100)
            else if (allStarCount >= 4)
            {
                fortune = "凶";
                comment = "お、落ち込まないで！いいことあるよ...！きっと！";
            }
            //大凶 (4 /100)
            else if (allStarCount >= 2)
            {
                fortune = "大凶";
                comment = "今日は身の周りに注意かも...";
            }
            //死 (1/100)
            else
            {
                fortune = "極凶";
                comment = "今日はおうちから出ないほうがいいかもね...？";
            }

            //ラッキーキャラ
            int charaCount = Directory.GetFiles("Resources/Chara", "*").Length;
            int participantsCount = Directory.GetFiles("Resources/Participants", "*").Length;
            num = rand.Next(charaCount + participantsCount);

            if (num < charaCount)
            {
                chara = "Chara/" + num;
            }
            else
            {
                chara = "Participants/" + (num - charaCount);
            }

            //ラッキー適正
            //バ場
            num = rand.Next(0, 5);

            field = GetRndFieldName(num);

            //距離
            num = rand.Next(0, 4);

            distance = GetRndDistanceName(num, GetRndFieldName(num));

            //脚質
            num = rand.Next(0, 4);

            leg = GetRndLegName(num);

            #endregion

            #region 出力

            float basePosX = 470,
                basePosY = 100;

            //ベースとなる画像読み込み
            Random rand2 = new Random(DateTime.Now.Second);
            System.Drawing.Image img = System.Drawing.Image.FromFile("Resources/Fortune/Base_" + rand2.Next(3) + ".png");
            System.Drawing.Image imgInshiBase = System.Drawing.Image.FromFile("Resources/Fortune/Inshi_Base_4.png");
            System.Drawing.Image imgInshiStar = System.Drawing.Image.FromFile("Resources/Fortune/Inshi_Star.png");
            System.Drawing.Image imgTekisei = System.Drawing.Image.FromFile("Resources/Fortune/Tekisei.png");
            System.Drawing.Image imgChara = System.Drawing.Image.FromFile("Resources/" + chara + ".png");

            //imageからグラフィック読み込み
            Graphics graphics = Graphics.FromImage(img);

            //フォント読み込み
            Font fntSmall = font30;
            Font fntNormal = font50;
            Font fntBig = font70;

            #region 運勢
            //位置計測
            SizeF strSize = graphics.MeasureString(authorName + "の今日の運勢...", fntNormal);

            //名前のサイズに合わせて拡縮
            float sx = strSize.Width > 550 ? 550.0f / strSize.Width : 1;
            graphics.TranslateTransform(basePosX, 0);
            graphics.ScaleTransform(sx, 1);
            //テキスト描画
            graphics.DrawString(authorName + "の今日の運勢...", fntNormal, Brushes.SaddleBrown, 0, basePosY);
            //拡縮を戻す
            graphics.ResetTransform();

            //色を変えて運勢描画
            Brush brush = Brushes.SaddleBrown;
            if (fortune == "超大吉")
            {
                brush = Brushes.Gold;
            }
            else if (fortune == "大大吉")
            {
                brush = Brushes.Orange;
            }
            else if (fortune == "大吉")
            {
                brush = Brushes.Red;
            }
            else if (fortune == "凶")
            {
                brush = Brushes.Blue;
            }
            else if (fortune == "大凶")
            {
                brush = Brushes.Navy;
            }
            else if (fortune == "極凶")
            {
                brush = Brushes.DarkRed;
            }

            graphics.DrawString(fortune, fntBig, brush, basePosX + strSize.Width * sx, basePosY - 10);
            #endregion

            #region 一言コメント

            graphics.DrawString(comment, fntSmall, Brushes.SaddleBrown, basePosX + 40, basePosY + 80);

            #endregion

            #region 因子
            //画像データ

            //定数
            const float STAR_LEFT_UP_POS_X = 697;
            const float STAR_LEFT_UP_POS_Y = 251;
            const float ADD_X = 32;
            const float ADD_Y = 70;

            //テクスチャに因子のベース画像を貼る
            graphics.DrawImage(imgInshiBase, 0, 0);

            //各運勢ごとに因子の★を貼る
            for (int i = 0; i < starCount.Length; i++)
            {
                //星の数だけループ
                for (int j = 0; j < starCount[i]; j++)
                {
                    //位置補正
                    float adjX = ADD_X * j + 350 * (i / 3), adjY = ADD_Y * (i % 3);

                    //★画像を貼る
                    graphics.DrawImage(imgInshiStar, STAR_LEFT_UP_POS_X + adjX, STAR_LEFT_UP_POS_Y + adjY, 26, 26);
                }
            }

            #endregion

            #region キャラ

            graphics.DrawString("ラッキーキャラ...", fntSmall, Brushes.SaddleBrown, basePosX, basePosY + 370);

            //画像貼り付け
            graphics.DrawImage(imgChara, 751, 443, 96, 96);

            #endregion

            #region 適性
            graphics.DrawString("ラッキー適性", fntSmall, Brushes.SaddleBrown, basePosX, basePosY + 430);

            //適性ベース画像貼り付け
            graphics.DrawImage(imgTekisei, 0, 0);

            //バ場適性
            if (field == "芝")
            {
                graphics.FillEllipse(Brushes.Red, 774, 587, 21, 21);
            }
            else if (field == "ダート")
            {
                graphics.FillEllipse(Brushes.Red, 912, 587, 21, 21);
            }

            //距離適性
            if (distance == "短距離")
            {
                graphics.FillEllipse(Brushes.Red, 774, 629, 21, 21);
            }
            else if (distance == "マイル")
            {
                graphics.FillEllipse(Brushes.Red, 912, 629, 21, 21);
            }
            else if (distance == "中距離")
            {
                graphics.FillEllipse(Brushes.Red, 1051, 629, 21, 21);
            }
            else if (distance == "長距離")
            {
                graphics.FillEllipse(Brushes.Red, 1189, 629, 21, 21);
            }

            //脚質適性
            if (leg == "逃げ")
            {
                graphics.FillEllipse(Brushes.Red, 774, 671, 21, 21);
            }
            else if (leg == "先行")
            {
                graphics.FillEllipse(Brushes.Red, 912, 671, 21, 21);
            }
            else if (leg == "差し")
            {
                graphics.FillEllipse(Brushes.Red, 1051, 671, 21, 21);
            }
            else if (leg == "追込")
            {
                graphics.FillEllipse(Brushes.Red, 1189, 671, 21, 21);
            }

            #endregion

            //保存
            img.Save(dstPath);

            //各種解放
            graphics.Dispose();
            img.Dispose();
            #endregion
        }

        /// <summary>
        /// 因子シミュレーション画像を作成
        /// </summary>
        private void CreateInshiImg(int speed, int stamina, int power, int guts, int wise, string skillName, string dstPath)
        {
            Random rand = new Random();

            //引数の情報から因子生成
            int nBlue = 0, nRed = 0, nGreen = 0;
            string strBlue = "", strRed = "", strGreen = "固有";

            //青
            //まずどの因子にするか
            int kind = rand.Next(5);
            int num = 0;
            //スピード
            if (kind == 0)
            {
                strBlue = "スピード";
                num = speed;
            }
            //スタミナ
            else if (kind == 1)
            {
                strBlue = "スタミナ";
                num = stamina;
            }
            //パワー
            else if (kind == 2)
            {
                strBlue = "パワー";
                num = power;
            }
            //根性
            else if (kind == 3)
            {
                strBlue = "根性";
                num = guts;
            }
            //賢さ
            else
            {
                strBlue = "賢さ";
                num = wise;
            }

            //星の数
            int inshi = rand.Next(1000);
            if (num < 600)
            {
                //☆ (90%)
                if (inshi < 900)
                {
                    nBlue = 1;
                }
                //☆☆ (10%)
                else
                {
                    nBlue = 2;
                }

            }
            else if (num < 1100)
            {
                //☆ (50%)
                if (inshi < 500)
                {
                    nBlue = 1;
                }
                //☆☆ (45%)
                else if (inshi < 950)
                {
                    nBlue = 2;
                }
                //☆☆☆ (5%)
                else
                {
                    nBlue = 3;
                }
            }
            else
            {
                //☆ (20%)
                if (inshi < 200)
                {
                    nBlue = 1;
                }
                //☆☆ (70%)
                else if (inshi < 900)
                {
                    nBlue = 2;
                }
                //☆☆☆ (10%)
                else
                {
                    nBlue = 3;
                }
            }


            //赤
            //まずどの因子にするか
            kind = rand.Next(10);
            //芝
            if (kind == 0)
            {
                strRed = "芝";
            }
            //ダート
            else if (kind == 1)
            {
                strRed = "ダート";
            }
            //短距離
            else if (kind == 2)
            {
                strRed = "短距離";
            }
            //マイル
            else if (kind == 3)
            {
                strRed = "マイル";
            }
            //中距離
            else if (kind == 4)
            {
                strRed = "中距離";
            }
            //長距離
            else if (kind == 5)
            {
                strRed = "長距離";
            }
            //逃げ
            else if (kind == 6)
            {
                strRed = "逃げ";
            }
            //先行
            else if (kind == 7)
            {
                strRed = "先行";
            }
            //差し
            else if (kind == 6)
            {
                strRed = "差し";
            }
            //追込
            else
            {
                strRed = "追込";
            }


            inshi = rand.Next(1000);
            //☆ (20%)
            if (inshi < 200)
            {
                nRed = 1;
            }
            //☆☆ (70%)
            else if (inshi < 900)
            {
                nRed = 2;
            }
            //☆☆☆ (10%)
            else
            {
                nRed = 3;
            }

            //緑
            if (skillName != "")
            {
                strGreen = skillName;
            }

            inshi = rand.Next(1000);
            //☆ (50%)
            if (inshi < 500)
            {
                nGreen = 1;
            }
            //☆☆ (42%)
            else if (inshi < 920)
            {
                nGreen = 2;
            }
            //☆☆☆ (8%)
            else
            {
                nGreen = 3;
            }


            //画像読み込み
            System.Drawing.Image imgBase = System.Drawing.Image.FromFile("Resources/Inshi/Base.png");
            System.Drawing.Image imgInshiStar = System.Drawing.Image.FromFile("Resources/Inshi/Star.png");

            //フォント読み込み
            Font font = font18;

            //imageからグラフィック読み込み
            Graphics graphics = Graphics.FromImage(imgBase);

            //文字や☆を描画
            //文字
            //青
            graphics.DrawString(strBlue, font, Brushes.White, 82, 206);
            //赤
            graphics.DrawString(strRed, font, Brushes.White, 82, 289);
            //緑
            graphics.DrawString(strGreen, font, Brushes.White, 82, 372);

            //☆
            int x = 582, addX = 32;
            //青
            for (int i = 0; i < nBlue; i++)
            {
                //★画像を貼る
                graphics.DrawImage(imgInshiStar, x + addX * i, 205, 26, 26);
            }
            //赤
            for (int i = 0; i < nRed; i++)
            {
                //★画像を貼る
                graphics.DrawImage(imgInshiStar, x + addX * i, 288, 26, 26);
            }
            //緑
            for (int i = 0; i < nGreen; i++)
            {
                //★画像を貼る
                graphics.DrawImage(imgInshiStar, x + addX * i, 371, 26, 26);
            }

            //保存
            imgBase.Save(dstPath);

            //各種解放
            graphics.Dispose();
            imgBase.Dispose();
        }
    }
}