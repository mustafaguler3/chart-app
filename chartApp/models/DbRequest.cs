using System;
namespace chartApp.models
{
	public class DbRequest
	{
        public string DbType { get; set; }
        public string Host { get; set; }
        public string Port { get; set; }
        public string DbName { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string Query { get; set; }
        public string ChartType { get; set; }
        public bool IsStoredProcedure { get; set; }
        public bool IsFunction { get; set; }
        public bool IsView { get; set; }
        public List<object> Parameters { get; set; } = new List<object>();
    }
}

