using Microsoft.EntityFrameworkCore;
using MobiShare.Core.Entities;
using MobiShare.Core.Enums;

namespace MobiShare.Infrastructure.Data
{
    public class MobiShareDbContext : DbContext
    {
        public MobiShareDbContext(DbContextOptions<MobiShareDbContext> options) : base(options)
        {
        }

        public DbSet<Utente> Utenti { get; set; }
        public DbSet<Mezzo> Mezzi { get; set; }
        public DbSet<Parcheggio> Parcheggi { get; set; }
        public DbSet<Slot> Slots { get; set; }
        public DbSet<DatiSensore> DatiSensori { get; set; }
        public DbSet<Corsa> Corse { get; set; }
        public DbSet<BuonoSconto> BuoniSconto { get; set; }
        public DbSet<StoricoPagementi> StoricoPagementi { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configurazione Utente
            modelBuilder.Entity<Utente>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Username).IsUnique();
                entity.HasIndex(e => e.Email).IsUnique();
                entity.Property(e => e.Credito).HasPrecision(10, 2);
                entity.Property(e => e.Tipo).HasConversion<string>();
                entity.Property(e => e.Stato).HasConversion<string>();
            });

            // Configurazione Mezzo
            modelBuilder.Entity<Mezzo>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Tipo).HasConversion<string>();
                entity.Property(e => e.Stato).HasConversion<string>();
                entity.Property(e => e.TariffaPerMinuto).HasPrecision(10, 2);
                entity.Property(e => e.Latitudine).HasPrecision(10, 7);
                entity.Property(e => e.Longitudine).HasPrecision(10, 7);

                entity.HasOne(m => m.ParcheggioDiPartenza)
                      .WithMany(p => p.MezziPresenti)
                      .HasForeignKey(m => m.ParcheggioDiPartenzaId)
                      .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(m => m.Slot)
                      .WithOne(s => s.MezzoPresente)
                      .HasForeignKey<Mezzo>(m => m.SlotId)
                      .OnDelete(DeleteBehavior.SetNull);
            });

            // Configurazione Parcheggio
            modelBuilder.Entity<Parcheggio>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Latitudine).HasPrecision(10, 7);
                entity.Property(e => e.Longitudine).HasPrecision(10, 7);
            });

            // Configurazione Slot
            modelBuilder.Entity<Slot>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Stato).HasConversion<string>();

                entity.HasOne(s => s.Parcheggio)
                      .WithMany(p => p.Slots)
                      .HasForeignKey(s => s.ParcheggiId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(e => new { e.ParcheggiId, e.Numero }).IsUnique();
            });

            // Configurazione DatiSensore
            modelBuilder.Entity<DatiSensore>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Colore).HasConversion<string>();

                entity.HasOne(ds => ds.Slot)
                      .WithOne(s => s.SensoreLuce)
                      .HasForeignKey<DatiSensore>(ds => ds.SlotId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // Configurazione Corsa
            modelBuilder.Entity<Corsa>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Costo).HasPrecision(10, 2);
                entity.Property(e => e.Stato).HasConversion<string>();

                entity.HasOne(c => c.Utente)
                      .WithMany(u => u.Corse)
                      .HasForeignKey(c => c.UtenteId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(c => c.Mezzo)
                      .WithMany(m => m.Corse)
                      .HasForeignKey(c => c.MezzoId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(c => c.ParcheggioDiPartenza)
                      .WithMany(p => p.CorsePartenza)
                      .HasForeignKey(c => c.ParcheggioDiPartenzaId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(c => c.ParcheggioDestinazione)
                      .WithMany(p => p.CorseDestinazione)
                      .HasForeignKey(c => c.ParcheggioDestinazioneId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // Configurazione BuonoSconto
            modelBuilder.Entity<BuonoSconto>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Valore).HasPrecision(10, 2);
                entity.Property(e => e.Stato).HasConversion<string>();

                entity.HasOne(bs => bs.Utente)
                      .WithMany(u => u.BuoniSconto)
                      .HasForeignKey(bs => bs.UtenteId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // Configurazione StoricoPagementi
            modelBuilder.Entity<StoricoPagementi>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Importo).HasPrecision(10, 2);

                entity.HasOne(sp => sp.Utente)
                      .WithMany(u => u.StoricoPagementi)
                      .HasForeignKey(sp => sp.UtenteId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(sp => sp.Corsa)
                      .WithMany()
                      .HasForeignKey(sp => sp.CorsaId)
                      .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(sp => sp.BuonoSconto)
                      .WithMany()
                      .HasForeignKey(sp => sp.BuonoScontoId)
                      .OnDelete(DeleteBehavior.SetNull);
            });

            // Seed data
            SeedData(modelBuilder);
        }

        private void SeedData(ModelBuilder modelBuilder)
        {
            // Seed Parcheggi
            var parcheggio1 = new Parcheggio
            {
                Id = "PARK_CENTRO",
                Nome = "Centro Città",
                Latitudine = 45.0703,
                Longitudine = 7.6869,
                Capacita = 10
            };

            var parcheggio2 = new Parcheggio
            {
                Id = "PARK_UNIVERSITA",
                Nome = "Università",
                Latitudine = 45.0625,
                Longitudine = 7.6626,
                Capacita = 15
            };

            var parcheggio3 = new Parcheggio
            {
                Id = "PARK_STAZIONE",
                Nome = "Stazione Ferroviaria",
                Latitudine = 45.0737,
                Longitudine = 7.6767,
                Capacita = 20
            };

            modelBuilder.Entity<Parcheggio>().HasData(parcheggio1, parcheggio2, parcheggio3);

            // Seed Slots
            var slots = new List<Slot>();
            var sensori = new List<DatiSensore>();

            // Slots per PARK_CENTRO
            for (int i = 1; i <= 10; i++)
            {
                var slotId = $"SLOT_CENTRO_{i:D2}";
                var slot = new Slot
                {
                    Id = slotId,
                    Numero = i,
                    ParcheggiId = "PARK_CENTRO",
                    Stato = StatoSlot.Libero
                };
                slots.Add(slot);

                var sensore = new DatiSensore
                {
                    Id = $"SENSOR_{slotId}",
                    SlotId = slotId,
                    Colore = ColoreLuce.Verde,
                    StatoAttivo = true
                };
                sensori.Add(sensore);
            }

            // Slots per PARK_UNIVERSITA
            for (int i = 1; i <= 15; i++)
            {
                var slotId = $"SLOT_UNI_{i:D2}";
                var slot = new Slot
                {
                    Id = slotId,
                    Numero = i,
                    ParcheggiId = "PARK_UNIVERSITA",
                    Stato = StatoSlot.Libero
                };
                slots.Add(slot);

                var sensore = new DatiSensore
                {
                    Id = $"SENSOR_{slotId}",
                    SlotId = slotId,
                    Colore = ColoreLuce.Verde,
                    StatoAttivo = true
                };
                sensori.Add(sensore);
            }

            // Slots per PARK_STAZIONE
            for (int i = 1; i <= 20; i++)
            {
                var slotId = $"SLOT_STAZ_{i:D2}";
                var slot = new Slot
                {
                    Id = slotId,
                    Numero = i,
                    ParcheggiId = "PARK_STAZIONE",
                    Stato = StatoSlot.Libero
                };
                slots.Add(slot);

                var sensore = new DatiSensore
                {
                    Id = $"SENSOR_{slotId}",
                    SlotId = slotId,
                    Colore = ColoreLuce.Verde,
                    StatoAttivo = true
                };
                sensori.Add(sensore);
            }

            modelBuilder.Entity<Slot>().HasData(slots);
            modelBuilder.Entity<DatiSensore>().HasData(sensori);

            // Seed Mezzi
            var mezzi = new List<Mezzo>
            {
                new Mezzo
                {
                    Id = "BIKE_001",
                    Tipo = TipoMezzo.BiciMuscolare,
                    Modello = "City Bike Classic",
                    TariffaPerMinuto = 0.15m,
                    ParcheggioDiPartenzaId = "PARK_CENTRO",
                    SlotId = "SLOT_CENTRO_01",
                    Latitudine = parcheggio1.Latitudine,
                    Longitudine = parcheggio1.Longitudine
                },
                new Mezzo
                {
                    Id = "BIKE_002",
                    Tipo = TipoMezzo.BiciElettrica,
                    Modello = "E-Bike Urban",
                    TariffaPerMinuto = 0.25m,
                    PercentualeBatteria = 85,
                    ParcheggioDiPartenzaId = "PARK_CENTRO",
                    SlotId = "SLOT_CENTRO_02",
                    Latitudine = parcheggio1.Latitudine,
                    Longitudine = parcheggio1.Longitudine
                },
                new Mezzo
                {
                    Id = "SCOOTER_001",
                    Tipo = TipoMezzo.Monopattino,
                    Modello = "Urban Scooter Pro",
                    TariffaPerMinuto = 0.30m,
                    PercentualeBatteria = 92,
                    ParcheggioDiPartenzaId = "PARK_UNIVERSITA",
                    SlotId = "SLOT_UNI_01",
                    Latitudine = parcheggio2.Latitudine,
                    Longitudine = parcheggio2.Longitudine
                },
                new Mezzo
                {
                    Id = "BIKE_003",
                    Tipo = TipoMezzo.BiciMuscolare,
                    Modello = "Mountain Bike",
                    TariffaPerMinuto = 0.20m,
                    ParcheggioDiPartenzaId = "PARK_STAZIONE",
                    SlotId = "SLOT_STAZ_01",
                    Latitudine = parcheggio3.Latitudine,
                    Longitudine = parcheggio3.Longitudine
                },
                new Mezzo
                {
                    Id = "BIKE_004",
                    Tipo = TipoMezzo.BiciElettrica,
                    Modello = "E-Bike Sport",
                    TariffaPerMinuto = 0.28m,
                    PercentualeBatteria = 67,
                    ParcheggioDiPartenzaId = "PARK_STAZIONE",
                    SlotId = "SLOT_STAZ_02",
                    Latitudine = parcheggio3.Latitudine,
                    Longitudine = parcheggio3.Longitudine
                }
            };

            modelBuilder.Entity<Mezzo>().HasData(mezzi);

            // Seed Utenti
            var adminUser = new Utente
            {
                Id = "ADMIN_001",
                Username = "admin",
                Email = "admin@mobishare.org",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin123"),
                Tipo = TipoUtente.Gestore,
                Credito = 0,
                Stato = StatoUtente.Attivo
            };

            var testUser = new Utente
            {
                Id = "USER_001",
                Username = "mario.rossi",
                Email = "mario@email.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("password123"),
                Tipo = TipoUtente.Cliente,
                Credito = 25.00m,
                PuntiEco = 150,
                Stato = StatoUtente.Attivo
            };

            modelBuilder.Entity<Utente>().HasData(adminUser, testUser);
        }
    }
}