using Newtonsoft.Json;
using System.Collections.Generic;
using Microsoft.Bot.Builder;
using System.Linq;
using Microsoft.BotBuilderSamples.Clu;

namespace Microsoft.BotBuilderSamples
{
    public partial class FlightBooking: IRecognizerConvert
    {
        public string Text;
        public string AlteredText;
        public enum Intent {
            BookFlight,
            Cancel,
            GetWeather,
            None
        };
        public Dictionary<Intent, IntentScore> Intents;

        public class _Entities
        {
            public CluEntity[] entities;

            public CluEntity[] fromCityList => entities.Where(e => e.Category == "fromCity").ToArray();

            public CluEntity[] toCityList => entities.Where(e => e.Category == "toCity").ToArray();

            public CluEntity[] flightDateList => entities.Where(e => e.Category == "flightDate").ToArray();

            public string fromCity => fromCityList.FirstOrDefault()?.Text;

            public string toCity => toCityList.FirstOrDefault()?.Text;

            public string flightDate => flightDateList.FirstOrDefault()?.Text;
        }

        public _Entities Entities;

        [JsonExtensionData(ReadData = true, WriteData = true)]
        public IDictionary<string, object> Properties {get; set; }

        public void Convert(dynamic result)
        {
            var app = JsonConvert.DeserializeObject<FlightBooking>(JsonConvert.SerializeObject(result, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore }));

            Text = app.Text;
            AlteredText = app.AlteredText;
            Intents = app.Intents;
            Entities = app.Entities;
            Properties = app.Properties;
        }

        public (Intent intent, double score) TopIntent()
        {
            Intent maxIntent = Intent.None;
            var max = 0.0;
            foreach (var entry in Intents)
            {
                if (entry.Value.Score > max)
                {
                    maxIntent = entry.Key;
                    max = entry.Value.Score.Value;
                }
            }
            return (maxIntent, max);
        }
    }
}
