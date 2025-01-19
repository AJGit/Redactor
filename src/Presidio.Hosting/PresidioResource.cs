// For ease of discovery, resource types should be placed in
// the Aspire.Hosting.ApplicationModel namespace. If there is
// likelihood of a conflict on the resource name consider using
// an alternative namespace.
namespace Aspire.Hosting.ApplicationModel;

public sealed class PresidioResource(string name) : ContainerResource(name) , IResourceWithConnectionString
{
    internal const string PresidioEndpointName = "presidio";

    // An EndpointReference is a core .NET Aspire type used for keeping
    // track of endpoint details in expressions. Simple literal values cannot
    // be used because endpoints are not known until containers are launched.
    private EndpointReference? _primaryEndpointReference;

    public EndpointReference PrimaryEndpoint =>  _primaryEndpointReference ??= new(this, PresidioEndpointName);

    // Required property on IResourceWithConnectionString. Represents a connection
    // string that applications can use to access the Presidio server. In this case
    // the connection string is composed of the endpoint reference.

    //public ReferenceExpression ConnectionStringExpression =>
    //    ReferenceExpression.Create(
    //        $"Endpoint={PrimaryEndpoint.Property(EndpointProperty.Scheme)}://{PrimaryEndpoint.Property(EndpointProperty.Host)}:{PrimaryEndpoint.Property(EndpointProperty.Port)}"
    //    );
    public ReferenceExpression ConnectionStringExpression =>
        ReferenceExpression.Create($"{PrimaryEndpoint.Property(EndpointProperty.Url)}");
}