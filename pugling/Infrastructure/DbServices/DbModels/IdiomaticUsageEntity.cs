﻿using pugling.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace pugling.Infrastructure.DbServices.DbModels
{
    public record IdiomaticUsageEntity : IIdiomaticUsage
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [MaxLength(500)]
        public string Phrase { get; set; } = string.Empty;

        [Required]
        [MaxLength(500)]
        public string Translation { get; set; } = string.Empty;

        public IdiomaticUsageEntity()
        {
        }

        public IdiomaticUsageEntity(string phrase, string translation)
        {
            Phrase = phrase;
            Translation = translation;
        }

        public IdiomaticUsageEntity(IIdiomaticUsage idiomaticUsage)
        {
            Phrase = idiomaticUsage.Phrase;
            Translation = idiomaticUsage.Translation;
        }
    }
}