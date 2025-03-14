using System;
using Intwenty.Entity;

namespace Intwenty.Model.Dto
{
    public class InformationHeader
    {

        public InformationHeader()
        {

        }

        public InformationHeader(InformationStatus entity)
        {
            Id = entity.Id;
            Version = entity.Version;
            ApplicationId = entity.ApplicationId;
            ChangedBy = entity.ChangedBy;
            ChangedDate = entity.ChangedDate;
            CreatedBy = entity.CreatedBy;
            OwnedBy = entity.OwnedBy;
            OwnedByOrganizationId = entity.OwnedByOrganizationId;
            OwnedByOrganizationName = entity.OwnedByOrganizationName;
            PerformDate = entity.PerformDate;
            StartDate = entity.StartDate;
            EndDate = entity.EndDate;
        }

        public int Id { get; set; }

        public int Version { get; set; }

        public string ApplicationId { get; set; }

        public string ChangedBy { get; set; }

        public string CreatedBy { get; set; }

        public string OwnedBy { get; set; }

        public string OwnedByOrganizationId { get; set; }

        public string OwnedByOrganizationName { get; set; }

        public DateTime ChangedDate { get; set; }

        public DateTime PerformDate { get; set; }

        public DateTime StartDate { get; set; }

        public DateTime EndDate { get; set; }
    }

  
}
