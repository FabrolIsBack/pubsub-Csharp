using System.Text;
using NATS.Client;
using NATS.Client.Core;


//https://www.nuget.org/profiles/NATS.io external packages to download by  .NET Cli command

public class NatsProvider
{
    private string accessToken;
    private string natsUrl;
    private string stream;
    private string jwt;

    private string tempPath;

    ConnectionFactory cf = new ConnectionFactory();
    IConnection nc;


    public NatsProvider(string _accessToken, string _natsUrl, string _stream)
    {
        accessToken = _accessToken;
        natsUrl = _natsUrl;
        stream = _stream;
    

    }

    public void Connect()
    {
        jwt = CreateAppJwt(accessToken);
        tempPath = System.IO.Path.GetTempFileName();
        FileStream fs = File.Create(tempPath);
        StreamWriter sw = new StreamWriter(fs);

        sw.WriteLine(jwt);
        sw.Flush();
        sw.Close();
        fs.Close();
        nc = cf.CreateConnection(natsUrl, tempPath);
    }

    public void Publish(string data)
    {
        nc.Publish(stream, Encoding.UTF8.GetBytes(data));
    }

    public void Publish(byte[] data)
    {
        nc.Publish(stream, data);
    }

    public void close()
    {
        nc.Close();
    }

    public String SubscribeSync()
    {
        Msg message = nc.SubscribeSync(stream, "queue").NextMessage();
        String msg = Encoding.UTF8.GetString(message.Data);
        Console.WriteLine("Messagge: " + msg);
        return msg;
    }

    private string CreateAppJwt(string seed)
    {
        NKeyPair account = NKeys.FromSeed(seed);
        String accPubkey = NKeys.PublicKeyFromSeed(seed);
        Dictionary<string, object> payload = new Dictionary<string, object>();
        String jti = GenerateJti();
        double iat = GenerateIat();
        String natsConfig = GetNatsConfig();

        payload.Add("jti", jti);
        payload.Add("iat", iat);

        payload.Add("iss", accPubkey);
        payload.Add("name", "developer");
        payload.Add("sub", accPubkey);
        payload.Add("nats", natsConfig);

      /*  Console.WriteLine("jti:" + jti);
        Console.WriteLine("iat:" + iat);
        Console.WriteLine("iss:" + accPubkey);
        Console.WriteLine("natsConfig:" + natsConfig);*/


        string sign = SignJwt(payload, account);
        string r = $"-----BEGIN NATS USER JWT-----{System.Environment.NewLine}{sign}{System.Environment.NewLine}------END NATS USER JWT------{System.Environment.NewLine}{System.Environment.NewLine}************************* IMPORTANT ************************{System.Environment.NewLine}NKEY Seed printed below can be used to sign and prove identity.{System.Environment.NewLine}NKEYs are sensitive and should be treated as secrets. {System.Environment.NewLine}{System.Environment.NewLine}-----BEGIN USER NKEY SEED-----{System.Environment.NewLine}{seed}{System.Environment.NewLine}------END USER NKEY SEED------{System.Environment.NewLine}{System.Environment.NewLine}*************************************************************";
        return r;
    }

    private String GenerateJti()
    {
        DateTimeOffset now = DateTimeOffset.Now;
        string unixTimeSeconds = Convert.ToString(now.ToUnixTimeSeconds());
        string random_number = new Random().NextInt64().ToString();
        string r = $"{unixTimeSeconds}{random_number}";
      //  Console.WriteLine("jti:" + r);
        return r;
    }

    private double GenerateIat()
    {
        DateTimeOffset now = DateTimeOffset.Now;
        int unixTimeSeconds = (int)now.ToUnixTimeSeconds();
        double r = (double)(unixTimeSeconds * 1000);
      //  Console.WriteLine("iat:" + r);
        return r;
    }

    private String GetNatsConfig()
    {
        return "{\"pub\":{},\"sub\":{},\"subs\":-1,\"data\":-1,\"payload\":-1,\"type\":\"user\",\"version\":2}";
    }

    private string SignJwt(Dictionary<string, object> payload, NKeyPair account)
    {
        Dictionary<string, string> header = new Dictionary<string, string> { { "typ", "JWT" }, { "alg", "ed25519-nkey" } };
        string headerString = DictionaryToString(header);
        string payloadString = DictionaryToString(payload);

        string header_encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes(headerString)).Replace("=", "");
        string payload_encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes(payloadString)).Replace("=", "");
        string jwtbase = header_encoded + "." + payload_encoded;
        string signature =  Convert.ToBase64String(account.Sign(Encoding.UTF8.GetBytes(jwtbase))).Replace("=", "").Replace("+", "-").Replace("/", "_");
     //   Console.WriteLine("signature:" + signature);
        return jwtbase + "." + signature;
    }

    public string DictionaryToString(Dictionary<string, object> dictionary)
    {
        string dictionaryString = "{";
        foreach (KeyValuePair<string, object> keyValues in dictionary)
        {
            if (keyValues.Key == "iat" || keyValues.Key == "nats")
                dictionaryString += $"\"{keyValues.Key}\":{keyValues.Value},";
            else
                dictionaryString += $"\"{keyValues.Key}\":\"{keyValues.Value}\",";

        }
        int l = dictionaryString.Length;
        return dictionaryString.Substring(0, l - 1) + "}";
    }

    public string DictionaryToString(Dictionary<string, string> dictionary)
    {
        string dictionaryString = "{";
        foreach (KeyValuePair<string, string> keyValues in dictionary)
        {
            dictionaryString += "\"" + keyValues.Key + "\":" + "\"" + keyValues.Value + "\",";
        }
        int l = dictionaryString.Length;
        return dictionaryString.Substring(0, l - 1) + "}";
    }
}
