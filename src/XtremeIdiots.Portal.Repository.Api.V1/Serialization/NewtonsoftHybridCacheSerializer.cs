using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Text;

using Microsoft.Extensions.Caching.Hybrid;

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace XtremeIdiots.Portal.Repository.Api.V1.Serialization
{
    /// <summary>
    /// Newtonsoft.Json-backed <see cref="IHybridCacheSerializer{T}"/> for types whose
    /// properties use <c>internal set</c> and <see cref="JsonPropertyAttribute"/> — patterns
    /// that System.Text.Json (the HybridCache default) cannot deserialize without extra
    /// source-generation or reflection configuration.
    /// </summary>
    /// <remarks>
    /// All Portal Repository DTOs are <c>record</c> types with <c>internal set</c> properties
    /// decorated with <c>[Newtonsoft.Json.JsonProperty]</c>. HybridCache's STJ fallback leaves
    /// every property at its default value on deserialization because the setters are not
    /// accessible. This serializer uses the same Newtonsoft settings as the host's MVC pipeline
    /// (string enums, ignore reference loops, allow non-public constructors) so cached bytes
    /// are byte-for-byte compatible with the wire format.
    /// </remarks>
    public sealed class NewtonsoftHybridCacheSerializer<T> : IHybridCacheSerializer<T>
    {
        internal static readonly JsonSerializerSettings Settings = new()
        {
            Converters = { new StringEnumConverter() },
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            ConstructorHandling = ConstructorHandling.AllowNonPublicDefaultConstructor,
            TypeNameHandling = TypeNameHandling.None,
            NullValueHandling = NullValueHandling.Include
        };

        public T Deserialize(ReadOnlySequence<byte> source)
        {
            var json = Encoding.UTF8.GetString(source);
            return JsonConvert.DeserializeObject<T>(json, Settings)!;
        }

        public void Serialize(T value, IBufferWriter<byte> target)
        {
            var json = JsonConvert.SerializeObject(value, Settings);
            var bytes = Encoding.UTF8.GetBytes(json);
            target.Write(bytes);
        }
    }

    /// <summary>
    /// <see cref="IHybridCacheSerializerFactory"/> that returns
    /// <see cref="NewtonsoftHybridCacheSerializer{T}"/> for every requested type.
    /// Registered in DI so that <em>all</em> types cached through
    /// <see cref="MX.Caching.Abstractions.IMxCache"/> in the V1 host use the Newtonsoft
    /// serializer rather than falling back to System.Text.Json.
    /// </summary>
    public sealed class NewtonsoftHybridCacheSerializerFactory : IHybridCacheSerializerFactory
    {
        public bool TryCreateSerializer<T>([NotNullWhen(true)] out IHybridCacheSerializer<T>? serializer)
        {
            serializer = new NewtonsoftHybridCacheSerializer<T>();
            return true;
        }
    }
}
