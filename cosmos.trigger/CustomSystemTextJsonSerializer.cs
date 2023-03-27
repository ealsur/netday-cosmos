using Azure.Core.Serialization;
using Microsoft.Azure.Cosmos;
using System.IO;
using System.Text.Json;

namespace cosmos.trigger
{
    public class CustomSystemTextJsonSerializer : CosmosSerializer
    {
        private readonly JsonObjectSerializer systemTextJsonSerializer;

        public CustomSystemTextJsonSerializer(JsonSerializerOptions jsonSerializerOptions)
        {
            this.systemTextJsonSerializer = new JsonObjectSerializer(jsonSerializerOptions);
        }

        public override T FromStream<T>(Stream stream)
        {
            using (stream)
            {
                if (stream.CanSeek
                       && stream.Length == 0)
                {
                    return default;
                }

                if (typeof(Stream).IsAssignableFrom(typeof(T)))
                {
                    return (T)(object)stream;
                }

                return (T)this.systemTextJsonSerializer.Deserialize(stream, typeof(T), default);
            }
        }

        public override Stream ToStream<T>(T input)
        {
            MemoryStream streamPayload = new MemoryStream();
            this.systemTextJsonSerializer.Serialize(streamPayload, input, input.GetType(), default);
            streamPayload.Position = 0;
            return streamPayload;
        }
    }
}
