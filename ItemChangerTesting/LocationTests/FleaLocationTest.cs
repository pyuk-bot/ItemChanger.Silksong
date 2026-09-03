using Benchwarp.Data;
using ItemChanger.Placements;
using ItemChanger.Silksong.RawData;

namespace ItemChangerTesting.LocationTests;

internal class FleaLocationTest : Test
{
    public override TestMetadata GetMetadata() => new()
    {
        Folder = TestFolder.LocationTests,
        MenuName = "Flea Location",
        MenuDescription = "Tests putting a debug item at each flea location",
        Revision = 2026020300,
    };

    public override void Setup(TestArgs args)
    {
        StartNear(SceneNames.Tut_02, PrimitiveGateNames.right1);

        foreach (string loc in Finder.LocationNames.Where(x => x.StartsWith("Flea-")))
        {
            Profile.AddPlacement(
                Finder
                .GetLocation(loc)!
                .Wrap()
                .WithDebugItem()
                );
        }

        // for use with mapwarp to quickly navigate to each flea scene
        Placement fleaFindings = Finder.GetLocation(LocationNames.Start)!.Wrap();
        foreach (string item in Finder.ItemNames.Where(x => x.StartsWith("Flea_Findings")))
        {
            fleaFindings.Add(Finder.GetItem(item)!);
        }
        Profile.AddPlacement(fleaFindings);
    }
}
