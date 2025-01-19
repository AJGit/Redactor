using Aspire.Hosting.ApplicationModel;

// ReSharper disable once CheckNamespace
namespace Aspire.Hosting;

public static class PresidioResourceBuilderExtensions
{
    /// <summary>
    /// Adds the <see cref="PresidioResource"/> to the given
    /// <paramref name="builder"/> instance. Uses the "2.1.0" tag.
    /// </summary>
    /// <param name="builder">The <see cref="IDistributedApplicationBuilder"/>.</param>
    /// <param name="name">The name of the resource.</param>
    /// <param name="httpPort">The HTTP port.</param>
    /// <returns>
    /// An <see cref="IResourceBuilder{Presidio}"/> instance that
    /// represents the added Presidio resource.
    /// </returns>
    public static IResourceBuilder<PresidioResource> AddPresidio(
        this IDistributedApplicationBuilder builder,
        [ResourceName] string name,
        int? httpPort = null)
    {
        ArgumentNullException.ThrowIfNull(builder, nameof(builder));
        ArgumentNullException.ThrowIfNull(name, nameof(name));

        var resource = new PresidioResource(name);

        return builder.AddResource(resource)
                .WithImage(PresidioContainerImageTags.Image)
                .WithImageRegistry(PresidioContainerImageTags.Registry)
                .WithAnnotation(new ContainerImageAnnotation { Image = PresidioContainerImageTags.Image, Tag = PresidioContainerImageTags.Tag, Registry = PresidioContainerImageTags.Registry })
                .WithImageTag(PresidioContainerImageTags.Tag)
                .WithEnvironment("PORT", "5001")
                .WithEnvironment("PATH", "/usr/local/bin:/usr/local/sbin:/usr/local/bin:/usr/sbin:/usr/bin:/sbin:/bin")
                .WithEnvironment("LANG", "C.UTF-8")
                .WithEnvironment("GPG_KEY", "E3FF2839C048B25C084DEBE9B26995E310250568")
                .WithEnvironment("PYTHON_VERSION", "3.9.21")
                .WithEnvironment("PYTHON_SHA256", "3126f59592c9b0d798584755f2bf7b081fa1ca35ce7a6fea980108d752a05bb1")
                .WithEnvironment("PIP_NO_CACHE_DIR", "1")
                .WithEnvironment("ANALYZER_CONF_FILE", "presidio_analyzer/conf/default_analyzer.yaml")
                .WithEnvironment("RECOGNIZER_REGISTRY_CONF_FILE", "presidio_analyzer/conf/default_recognizers.yaml")
                .WithEnvironment("NLP_CONF_FILE", "presidio_analyzer/conf/default.yaml")
                .WithHttpEndpoint(
                    targetPort: 5001,
                    port: httpPort,
                    name: PresidioResource.PresidioEndpointName)
                //.ExcludeFromManifest()
                .WithExternalHttpEndpoints()
            ;
    }
}