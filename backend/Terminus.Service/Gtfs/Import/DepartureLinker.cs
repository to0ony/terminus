using System.Text;
using Terminus.Model.Common;

namespace Terminus.Service.Gtfs.Import;

/// <summary>
/// Povezuje vožnje istog polaska u različitim kalendarima (DepartureId), da se u
/// grafikonu mijenjaju zajedno. Grafikon pri izmjeni kopira vremena, natpise,
/// trasu i pristupačnost na sve vožnje polaska, pa se povezuju samo vožnje u
/// kojima je sve to isto. Vozilo (block_id) smije biti različito.
/// </summary>
internal static class DepartureLinker
{
    /// <returns>Broj polazaka koji voze u barem dva kalendara.</returns>
    public static int Link(GtfsFeedData feed)
    {
        var stopTimes = feed.StopTimes.ToLookup(st => st.TripId);
        var departures = 0;

        foreach (var same in feed.Trips.GroupBy(t => Signature(t, stopTimes[t.Id])))
        {
            // Polazak ima najviše jednu vožnju po kalendaru; dvije iste vožnje istog
            // kalendara (npr. pojačanje) su dva polaska.
            var byCalendar = same.GroupBy(t => t.ServiceCalendarId).Select(g => g.ToList()).ToList();
            if (byCalendar.Count < 2)
            {
                continue;
            }

            for (var i = 0; ; i++)
            {
                var trips = byCalendar.Where(g => i < g.Count).Select(g => g[i]).ToList();
                if (trips.Count < 2)
                {
                    break;
                }

                var departureId = Guid.CreateVersion7();
                trips.ForEach(t => t.DepartureId = departureId);
                departures++;
            }
        }

        return departures;
    }

    private static string Signature(ITripModel trip, IEnumerable<IStopTimeModel> stopTimes)
    {
        var signature = new StringBuilder()
            .AppendJoin('|', trip.RouteId, trip.VariantId, trip.DirectionId, trip.Headsign, trip.ShortName, trip.ShapeId, trip.WheelchairAccessible, trip.BikesAllowed);

        foreach (var st in stopTimes.OrderBy(st => st.StopSequence))
        {
            signature.Append('|').AppendJoin(',',
                st.StopId, st.ArrivalTime, st.DepartureTime, st.StopHeadsign, st.PickupType, st.DropOffType,
                st.ContinuousPickup, st.ContinuousDropOff, st.Timepoint);
        }

        return signature.ToString();
    }
}
