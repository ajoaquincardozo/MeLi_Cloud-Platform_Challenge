using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json.Serialization;
using System.Text.Json;
using System.Threading.Tasks;

namespace MeLi.UrlShortener.Infrastructure.Cache.Converters
{
    public class PrivateSetterJsonConverter<T> : JsonConverter<T>
    {
        public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            // Creamos una instancia usando el constructor sin parámetros
            var instance = Activator.CreateInstance(typeToConvert, true);
            var jsonDocument = JsonDocument.ParseValue(ref reader);
            var jsonObject = jsonDocument.RootElement;

            foreach (var property in typeToConvert.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                // Verificar si la propiedad tiene un setter privado
                var setter = property.GetSetMethod(true);
                if (setter == null) continue;

                if (jsonObject.TryGetProperty(property.Name, out var value))
                {
                    var convertedValue = JsonSerializer.Deserialize(
                        value.GetRawText(),
                        property.PropertyType,
                        options);

                    setter.Invoke(instance, new[] { convertedValue });
                }
            }

            return (T)instance;
        }

        public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
        {
            var newOptions = new JsonSerializerOptions(options);
            newOptions.Converters.Remove(this);
            JsonSerializer.Serialize(writer, value, newOptions);
        }
    }
}
