using PolyType;

namespace Samples.RpcBenchmark;

[GenerateShapeFor<HelloRequest>]
[GenerateShapeFor<HelloReply>]
[GenerateShapeFor<User>]
[GenerateShapeFor<Item>]
[GenerateShapeFor<GetItemsRequest>]
internal partial class NerdbankShapesWitness;
