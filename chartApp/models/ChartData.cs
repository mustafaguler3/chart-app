using System;
namespace chartApp.models
{
	public class ChartData
	{
        public List<string> Labels { get; set; }
        public Dictionary<string, List<object>> Values { get; set; }

        public ChartData()
        {
            Labels = new List<string>();
            Values = new Dictionary<string, List<object>>();
        }
    }
}

