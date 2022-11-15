namespace DnsTube.Core.Models.Zone
{
    public class ListZonesResponse
    {
        public bool success { get; set; }
        public object[] errors { get; set; }
        public object[] messages { get; set; }
        public Result[] result { get; set; }
        public Result_Info result_info { get; set; }
    }

    public class Result_Info
    {
        public int page { get; set; }
        public int per_page { get; set; }
        public int count { get; set; }
        public int total_count { get; set; }
    }

    public class Result
    {
        public string id { get; set; }
        public string name { get; set; }
        public int development_mode { get; set; }
        public string[] original_name_servers { get; set; }
        public string original_registrar { get; set; }
        public string original_dnshost { get; set; }
        public DateTime created_on { get; set; }
        public DateTime modified_on { get; set; }
        public Owner owner { get; set; }
        public string[] permissions { get; set; }
        public Plan plan { get; set; }
        public Plan_Pending plan_pending { get; set; }
        public string status { get; set; }
        public bool paused { get; set; }
        public string type { get; set; }
        public string[] name_servers { get; set; }
    }

    public class Owner
    {
        public string id { get; set; }
        public string email { get; set; }
        public string owner_type { get; set; }
    }

    public class Plan
    {
        public string id { get; set; }
        public string name { get; set; }
        public int price { get; set; }
        public string currency { get; set; }
        public string frequency { get; set; }
        public string legacy_id { get; set; }
        public bool is_subscribed { get; set; }
        public bool can_subscribe { get; set; }
    }

    public class Plan_Pending
    {
        public string id { get; set; }
        public string name { get; set; }
        public int price { get; set; }
        public string currency { get; set; }
        public string frequency { get; set; }
        public string legacy_id { get; set; }
        public bool is_subscribed { get; set; }
        public bool can_subscribe { get; set; }
    }
}
