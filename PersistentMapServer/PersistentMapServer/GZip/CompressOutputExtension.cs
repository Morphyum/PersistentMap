using System.ServiceModel;

namespace PersistentMapServer {
    class CompressOutputExtension : IExtension<OperationContext> {

        public void Attach(OperationContext owner) {}

        public void Detach(OperationContext owner) {}
    }
}
