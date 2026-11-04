namespace ServiceLib.Tests.Handler;

public class SubscriptionInfoParserTests
{
    [Test]
    public async Task Parse_ShouldReadStandardHeaders()
    {
        var headers = new Dictionary<string, string>
        {
            ["Subscription-Userinfo"] = "upload=1073741824; download=2147483648; total=53687091200; expire=1791936000",
            ["profile-title"] = "base64:" + Convert.ToBase64String(Encoding.UTF8.GetBytes("RockBlack Премиум")),
            ["support-url"] = "https://t.me/rockblack_support",
            ["profile-web-page-url"] = "https://my.rockblack.tech",
            ["announce"] = "Maintenance tonight",
            ["profile-update-interval"] = "12",
            ["content-type"] = "text/plain",
        };

        var info = SubscriptionInfoParser.Parse("sub1", headers, "vless://abc@example.com:443#DE");

        await info.Id.Should().BeEqualTo("sub1");
        await info.HasUserInfo.Should().BeTrue();
        await info.Upload.Should().BeEqualTo(1073741824L);
        await info.Download.Should().BeEqualTo(2147483648L);
        await info.Used.Should().BeEqualTo(3221225472L);
        await info.Total.Should().BeEqualTo(53687091200L);
        await info.Expire.Should().BeEqualTo(1791936000L);
        await info.Title.Should().BeEqualTo("RockBlack Премиум");
        await info.SupportUrl.Should().BeEqualTo("https://t.me/rockblack_support");
        await info.WebPageUrl.Should().BeEqualTo("https://my.rockblack.tech");
        await info.Announce.Should().BeEqualTo("Maintenance tonight");
        await info.UpdateIntervalHours.Should().BeEqualTo(12);
        await info.IsEmpty.Should().BeFalse();
    }

    [Test]
    public async Task Parse_ShouldReadCommentLinesFromPlainBody()
    {
        const string body = """
            #profile-title: My subscription
            #subscription-userinfo: upload=0; download=3221225472; total=0; expire=0
            #support-url: https://t.me/help
            #announce: base64:SGVsbG8=
            vless://uuid@de.example.com:443?security=reality#🇩🇪 Germany
            vless://uuid@nl.example.com:443#🇳🇱 Netherlands
            """;

        var info = SubscriptionInfoParser.Parse("s", null, body);

        await info.Title.Should().BeEqualTo("My subscription");
        await info.HasUserInfo.Should().BeTrue();
        await info.Download.Should().BeEqualTo(3221225472L);
        await info.Total.Should().BeEqualTo(0L);
        await info.SupportUrl.Should().BeEqualTo("https://t.me/help");
        await info.Announce.Should().BeEqualTo("Hello");
    }

    [Test]
    public async Task Parse_ShouldReadCommentLinesFromBase64Body()
    {
        var plain = "#profile-title: Encoded\nvless://uuid@x.example.com:443#X\n";
        var body = Convert.ToBase64String(Encoding.UTF8.GetBytes(plain));

        var info = SubscriptionInfoParser.Parse("s", null, body);

        await info.Title.Should().BeEqualTo("Encoded");
    }

    [Test]
    public async Task Parse_HeadersShouldWinOverBody()
    {
        var headers = new Dictionary<string, string> { ["profile-title"] = "From header" };

        var info = SubscriptionInfoParser.Parse("s", headers, "#profile-title: From body\n");

        await info.Title.Should().BeEqualTo("From header");
    }

    [Test]
    public async Task Parse_WithoutMetadata_ShouldBeEmpty()
    {
        var info = SubscriptionInfoParser.Parse("s", new Dictionary<string, string> { ["content-type"] = "text/plain" }, "vless://a@b:1#c");

        await info.IsEmpty.Should().BeTrue();
        await info.HasUserInfo.Should().BeFalse();
    }

    [Test]
    [Arguments("upload=10;download=20;total=30;expire=40", 10L, 20L, 30L, 40L)]
    [Arguments(" upload = 1 ; download = 2 ; total = 3 ", 1L, 2L, 3L, 0L)]
    [Arguments("upload=1.5e3; download=0; total=1024; expire=0", 1500L, 0L, 1024L, 0L)]
    public async Task ApplyUserInfo_ShouldParseVariants(string value, long upload, long download, long total, long expire)
    {
        var item = new SubInfoItem();

        SubscriptionInfoParser.ApplyUserInfo(item, value);

        await item.HasUserInfo.Should().BeTrue();
        await item.Upload.Should().BeEqualTo(upload);
        await item.Download.Should().BeEqualTo(download);
        await item.Total.Should().BeEqualTo(total);
        await item.Expire.Should().BeEqualTo(expire);
    }

    [Test]
    public async Task ApplyUserInfo_ShouldIgnoreGarbage()
    {
        var item = new SubInfoItem();

        SubscriptionInfoParser.ApplyUserInfo(item, "hello world");

        await item.HasUserInfo.Should().BeFalse();
    }

    [Test]
    public async Task DownloadService_ShouldExposeResponseHeadersForParsing()
    {
        await CertPemManager.Instance.Init(new Config { GuiItem = new GUIItem() });
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var url = $"http://127.0.0.1:{((IPEndPoint)listener.LocalEndpoint).Port}/sub";
        const string body = "#announce: hello\nvless://uuid@de.example.com:443#DE\n";
        var serve = Task.Run(async () =>
        {
            using var client = await listener.AcceptTcpClientAsync();
            await using var stream = client.GetStream();
            using var reader = new StreamReader(stream, Encoding.ASCII, leaveOpen: true);
            while (await reader.ReadLineAsync() is { Length: > 0 })
            {
            }
            var response = "HTTP/1.1 200 OK\r\n"
                           + "subscription-userinfo: upload=1; download=2; total=3; expire=4\r\n"
                           + "profile-title: base64:" + Convert.ToBase64String(Encoding.UTF8.GetBytes("Titel")) + "\r\n"
                           + "support-url: https://t.me/help\r\n"
                           + $"Content-Length: {Encoding.UTF8.GetByteCount(body)}\r\nConnection: close\r\n\r\n" + body;
            await stream.WriteAsync(Encoding.UTF8.GetBytes(response));
        });

        try
        {
            var service = new DownloadService();
            var content = await service.TryDownloadString(url, (IWebProxy?)null, "test").WaitAsync(TimeSpan.FromSeconds(20));
            await serve;

            await content.Should().BeEqualTo(body);
            await service.LastResponseHeaders.Should().NotBeNull();
            var info = SubscriptionInfoParser.Parse("s", service.LastResponseHeaders, content);
            await info.Total.Should().BeEqualTo(3L);
            await info.Expire.Should().BeEqualTo(4L);
            await info.Title.Should().BeEqualTo("Titel");
            await info.SupportUrl.Should().BeEqualTo("https://t.me/help");
            await info.Announce.Should().BeEqualTo("hello");
        }
        finally
        {
            listener.Stop();
        }
    }

    [Test]
    public async Task Merge_ShouldKeepKnownValuesAndDropOldAnnouncement()
    {
        var stored = new SubInfoItem { Id = "s", Title = "Old title", SupportUrl = "https://t.me/a", Announce = "old", HasUserInfo = true, Total = 5 };
        var fresh = new SubInfoItem { Id = "s", HasUserInfo = true, Total = 10 };

        var merged = SubscriptionInfoParser.Merge(stored, fresh);

        await merged.Title.Should().BeEqualTo("Old title");
        await merged.SupportUrl.Should().BeEqualTo("https://t.me/a");
        await merged.Announce.Should().BeNull();
        await merged.Total.Should().BeEqualTo(10L);
    }
}
