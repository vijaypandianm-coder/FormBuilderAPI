using System;
using System.Collections.Generic;

namespace FormBuilderAPI.DTOs
{
    // ───────── existing submit payloads ─────────
    public class SubmitAnswerDto
    {
        public string FieldId { get; set; } = default!;
        public string? AnswerValue { get; set; }
        public List<string>? OptionIds { get; set; }
        public string? FileName { get; set; }
        public string? ContentType { get; set; }          // e.g., "image/png"
        public string? FileBase64 { get; set; }           // base64 data only, no data: prefix
    }

    public class SubmitResponseDto
    {
        public List<SubmitAnswerDto> Answers { get; set; } = new();
    }

    // ───────── new lightweight view DTOs ─────────
    public class ResponseHeaderDto
    {
        public long Id { get; set; }
        public int FormKey { get; set; }
        public long UserId { get; set; }
        public DateTime SubmittedAt { get; set; }
    }

    public class ResponseAnswerRow
    {
        public long Id { get; set; }
        public long ResponseId { get; set; }
        public string FieldId { get; set; } = default!;
        public string? FieldType { get; set; }
        public string? AnswerValue { get; set; }
        public DateTime SubmittedAt { get; set; }
    }

    public class ResponseAnswerDto
    {
        public string FieldId { get; set; } = default!;
        public string? FieldType { get; set; }
        public string? AnswerValue { get; set; }
    }

    public class ResponseDetailDto
    {
        public ResponseHeaderDto Header { get; set; } = default!;
        public List<ResponseAnswerDto> Answers { get; set; } = new();
    }

    public class PublishedFormDto
    {
        public int FormKey { get; set; }
        public string Title { get; set; } = "";
        public string? Description { get; set; }
        public DateTime? PublishedAt { get; set; }
    }
}