// Models/SqlModels/FormResponse.cs
using System;

namespace FormBuilderAPI.Models.SqlModels
{
    public class FormResponse
    {
        public long   Id { get; set; }
        public string FormId  { get; set; } = default!;
        public int    FormKey { get; set; }
        public long   UserId  { get; set; }
        public DateTime SubmittedAt { get; set; }

        
    }
}