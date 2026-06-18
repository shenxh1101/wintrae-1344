using System;
using System.Collections.Generic;

namespace FinanceReimbursement.Models
{
    public class Trip
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();

        public string TripNo { get; set; } = string.Empty;

        public string Destination { get; set; } = string.Empty;

        public string DestinationCity { get; set; } = string.Empty;

        public CityLevel? CityLevel { get; set; }

        public DateTime DepartureDate { get; set; }

        public DateTime ReturnDate { get; set; }

        public int Days
        {
            get
            {
                if (ReturnDate < DepartureDate) return 0;
                return (int)Math.Ceiling((ReturnDate.Date - DepartureDate.Date).TotalDays) + 1;
            }
        }

        public TransportationType? TransportationTo { get; set; }

        public TransportationType? TransportationBack { get; set; }

        public string Purpose { get; set; } = string.Empty;

        public string ProjectId { get; set; } = string.Empty;

        public string ProjectName { get; set; } = string.Empty;

        public string CustomerId { get; set; } = string.Empty;

        public string CustomerName { get; set; } = string.Empty;

        public string Remarks { get; set; } = string.Empty;
    }
}
