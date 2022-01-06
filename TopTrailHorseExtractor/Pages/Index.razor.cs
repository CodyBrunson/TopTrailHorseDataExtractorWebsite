using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Transactions;
using DataJuggler.Blazor.FileUpload;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using TopTrailHorseExtractor.Models;

namespace TopTrailHorseExtractor.Pages;

public partial class Index
{
    private List<Ride> RideData { get; set; }
    private List<string> HorseNames { get; set; }
    private List<string> Years { get; set; }
    private Dictionary<string, List<Ride>> RidesSortedByYear { get; set; }
    private Dictionary<string, List<Ride>> RidesSortedByHorse { get; set; }
    private List<Ride> CurrentlySelectedHorseRides { get; set; }
    
    private string _totalDistance = String.Empty;
    private double _totalDistanceForYear = 0;
    private bool _viewYearTotals = false;
    private Ride _longestRide = new();
    private string _currentlySelectedHorse = String.Empty;

    public async Task OnUploadData(InputFileChangeEventArgs e)
    {
        var uploadedFile = e.File;
        var regex = new Regex(".+\\.csv", RegexOptions.Compiled);
        if (regex.IsMatch(uploadedFile.Name))
        {
            RideData ??= new List<Ride>();
            RideData?.Clear();
            var stream = uploadedFile.OpenReadStream();
            var ms = new MemoryStream();
            await stream.CopyToAsync(ms);
            stream.Close();
            var outputFileString = Encoding.UTF8.GetString(ms.ToArray());

            foreach (var item in outputFileString.Split(Environment.NewLine))
            {
                if (string.IsNullOrEmpty(item)) break;
                if (item.Contains("Ride ID")) continue;
                
                var lineData = item.Replace("\"","").Split(',').ToList();
                var ride = new Ride()
                {
                    RideId = Convert.ToInt32(lineData[0]),
                    UserId = Convert.ToInt32(lineData[1]),
                    RideDate = lineData[2],
                    UploadDate = lineData[3],
                    Distance = Convert.ToDouble(lineData[4]),
                    Unused = lineData[5],
                    Duration = lineData[6],
                    RideName = lineData[7],
                    Difficulty = lineData[8],
                    Privacy = lineData[9],
                    PublicAccess = lineData[10],
                    Journal = lineData[11],
                    Horse = lineData[12].Trim(),
                };
                RideData?.Add(ride);
            }

            PopulateHorseNames();
            PopulateYears();
            PopulateRidesSortedByYear();
            PopulateRidesSortedByHorseName();
            CalculateTotalRideDistance();
        }
    }

    private void ToggleYearlyTotals(MouseEventArgs e)
    {
        _viewYearTotals = !_viewYearTotals;
    }
    private void PopulateHorseNames()
    {
        var temp = new List<string>();
        foreach (var ride in RideData.Where(ride => !temp.Contains(ride.Horse)))
        {
            temp.Add(ride.Horse);
        }

        HorseNames = temp;
    }

    private void PopulateRidesByCurrentlySelectedHorse(ChangeEventArgs e)
    {
        _currentlySelectedHorse = e.Value.ToString();
        CurrentlySelectedHorseRides = RidesSortedByHorse[_currentlySelectedHorse];
    }
    private void PopulateRidesSortedByHorseName()
    {
        var temp = new Dictionary<string, List<Ride>>();
        foreach (var ride in RideData)
        {
            if (!temp.ContainsKey(ride.Horse))
                temp.TryAdd(ride.Horse, new List<Ride> {ride});
            else
                temp[ride.Horse].Add(ride);
        }

        RidesSortedByHorse = temp;
    }

    private string GetTotalMilesForYearByHorse()
    {
        var SB = new StringBuilder();
        foreach (var year in Years)
        {
            var temp = 0.0d;
            foreach (var ride in RidesSortedByYear[year])
            {
                if (ride.Horse == _currentlySelectedHorse)
                {
                    temp += ride.Distance;
                }
            }

            SB.Append(year + ": " + temp.ToString("N2") + " miles - ");
        }

        return SB.ToString();
    }

    private string GetTotalMilesByHorse(List<Ride> rides)
    {
        var temp = 0.0d;
        foreach (var ride in rides)
        {
            temp += ride.Distance;
        }
        return temp.ToString("N2") + " miles";
    }
    private string GetLongestRodeMiles(List<Ride> rides)
    {
        var temp = 0.0d;
        foreach (var ride in rides)
        {
            if (temp < ride.Distance)
            {
                temp = ride.Distance;
            }
        }

        return temp.ToString("N2") + " miles";
    }
    private void PopulateRidesSortedByYear()
    {
        var temp = new Dictionary<string, List<Ride>>();
        foreach (var ride in RideData)
        {
            if (!temp.ContainsKey(ride.RideDate[..4]))
                temp.TryAdd(ride.RideDate[..4], new List<Ride> { ride });
            else
                temp[ride.RideDate[..4]].Add(ride);
        }
        
        RidesSortedByYear = temp;
    }

    private void PopulateYears()
    {
        var temp = new List<string>();
        foreach (var ride in RideData.Where(ride => !temp.Contains(ride.RideDate[..4])))
        {
            temp.Add(ride.RideDate[..4]);
        }

        Years = temp;
    }
    private void CalculateTotalRideDistance()
    {
        var temp = 0.0d;
        foreach (var ride in RideData)
        {
            temp += ride.Distance;
        }

        _totalDistance = temp.ToString("N2");
    }
}