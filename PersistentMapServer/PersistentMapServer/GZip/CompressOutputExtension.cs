using System.ServiceModel;

namespace PersistentMapServer {

    // Marker that indicates the current request asked for a compressed response.
    class CompressOutputExtension : IExtension<OperationContext> {

        public void Attach(OperationContext owner) {}

        public void Detach(OperationContext owner) {}
    }
}
