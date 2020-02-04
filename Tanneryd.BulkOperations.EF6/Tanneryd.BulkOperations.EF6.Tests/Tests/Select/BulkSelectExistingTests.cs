﻿/*
* Copyright ©  2017-2019 Tånneryd IT AB
* 
* Licensed under the Apache License, Version 2.0 (the "License");
* you may not use this file except in compliance with the License.
* You may obtain a copy of the License at
* 
*   http://www.apache.org/licenses/LICENSE-2.0
* 
* Unless required by applicable law or agreed to in writing, software
* distributed under the License is distributed on an "AS IS" BASIS,
* WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
* See the License for the specific language governing permissions and
* limitations under the License.
*/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Tanneryd.BulkOperations.EF6.NetStd;
using Tanneryd.BulkOperations.EF6.NetStd.Model;
using Tanneryd.BulkOperations.EF6.Tests.DM.Numbers;
using Tanneryd.BulkOperations.EF6.Tests.DM.Teams.UsingUserGeneratedGuidKeys;
using Tanneryd.BulkOperations.EF6.Tests.EF;
using Tanneryd.BulkOperations.EF6.Tests.Models.DM.Prices;

namespace Tanneryd.BulkOperations.EF6.Tests.Tests.Select
{
    [TestClass]
    public class BulkSelectExistingTests : BulkOperationTestBase
    {
        [TestInitialize]
        public void Initialize()
        {
            InitializeNumberContext();
            InitializeTeamContext();
            InitializePriceContext();
            CleanUp();
        }

        [TestCleanup]
        public void CleanUp()
        {
            CleanupNumberContext();
            CleanupTeamContext();
            CleanupPriceContext();
        }

        [TestMethod]
        public void ZeroShouldNotMatchNullWhenSelectExisting()
        {
            using (var db = new PriceContext())
            {
                db.Prices.Add(new Price() { Date = new DateTime(2019, 1, 1), Name = "ERICB", Value = 80 });
                db.Prices.Add(new Price() { Date = new DateTime(2019, 1, 2), Name = "ERICB", Value = 81 });
                db.Prices.Add(new Price() { Date = new DateTime(2019, 1, 3), Name = "ERICB", Value = 82 });
                db.Prices.Add(new Price() { Date = new DateTime(2019, 1, 4), Name = "ERICB", Value = 0 });
                db.Prices.Add(new Price() { Date = new DateTime(2019, 1, 5), Name = "ERICB", Value = 86 });
                db.SaveChanges();

                var prices = new[]
                {
                    db.Prices.Add(new Price() {Date = new DateTime(2019, 1, 1), Name = "ERICB", Value = 80}),
                    db.Prices.Add(new Price() {Date = new DateTime(2019, 1, 2), Name = "ERICB", Value = 81}),
                    db.Prices.Add(new Price() {Date = new DateTime(2019, 1, 3), Name = "ERICB", Value = 82}),
                    db.Prices.Add(new Price() {Date = new DateTime(2019, 1, 4), Name = "ERICB", Value = null}),
                    db.Prices.Add(new Price() {Date = new DateTime(2019, 1, 5), Name = "ERICB", Value = 86})
                };
                var existing = db.BulkSelectExisting<Price, Price>(
                    new BulkSelectRequest<Price>(new[] { "Date", "Name", "Value" }, prices));
                Assert.AreEqual(4, existing.Count);
                Assert.AreSame(prices[0], existing[0]);
                Assert.AreSame(prices[1], existing[1]);
                Assert.AreSame(prices[2], existing[2]);
                Assert.AreSame(prices[4], existing[3]);
            }
        }

        [TestMethod]
        public void ZeroShouldNotMatchNullWhenSelectNotExisting()
        {
            using (var db = new PriceContext())
            {
                db.Prices.Add(new Price() { Date = new DateTime(2019, 1, 1), Name = "ERICB", Value = 80 });
                db.Prices.Add(new Price() { Date = new DateTime(2019, 1, 2), Name = "ERICB", Value = 81 });
                db.Prices.Add(new Price() { Date = new DateTime(2019, 1, 3), Name = "ERICB", Value = 82 });
                db.Prices.Add(new Price() { Date = new DateTime(2019, 1, 4), Name = "ERICB", Value = 0 });
                db.Prices.Add(new Price() { Date = new DateTime(2019, 1, 5), Name = "ERICB", Value = 86 });
                db.SaveChanges();

                var prices = new[]
                {
                    db.Prices.Add(new Price() {Date = new DateTime(2019, 1, 1), Name = "ERICB", Value = 80}),
                    db.Prices.Add(new Price() {Date = new DateTime(2019, 1, 2), Name = "ERICB", Value = 81}),
                    db.Prices.Add(new Price() {Date = new DateTime(2019, 1, 3), Name = "ERICB", Value = 82}),
                    db.Prices.Add(new Price() {Date = new DateTime(2019, 1, 4), Name = "ERICB", Value = null}),
                    db.Prices.Add(new Price() {Date = new DateTime(2019, 1, 5), Name = "ERICB", Value = 86})
                };
                var existing = db.BulkSelectNotExisting<Price, Price>(
                    new BulkSelectRequest<Price>(new[] { "Date", "Name", "Value" }, prices));
                Assert.AreEqual(1, existing.Count);
                Assert.AreSame(prices[3], existing[0]);
            }
        }

        [TestMethod]
        public void SelectExistingFromTableWithUserGeneratedGuidAsPrimaryKey()
        {
            using (var db = new UserGeneratedTeamContext())
            {
                var teams = new List<Team>();

                // Add ten teams to the database (Team 0 - Team 9)
                for (int i = 0; i < 10; i++)
                {
                    teams.Add(new Team { Id = Guid.NewGuid(), Name = $"Team #{i}" });
                }

                // Save the ten first teams to the database.
                db.BulkInsertAll(new BulkInsertRequest<Team>
                {
                    Entities = teams,
                });

                // Add another ten teams (Team 10 - Team 19) to
                // the list but not to the database.
                for (int i = 10; i < 20; i++)
                {
                    teams.Add(new Team { Id = Guid.NewGuid(), Name = $"Team #{i}" });
                }

                // The only teams we should get back out of the 20 teams (Team 0 - Team 19)
                // are the first ten that we saved to the database.
                var existingTeams = db.BulkSelectExisting<Team, Team>(new BulkSelectRequest<Team>(new[] { "Id" }, teams));
                existingTeams = existingTeams.OrderBy(t => t.Name).ToList();
                Assert.AreEqual(10, existingTeams.Count);
                for (int i = 0; i < 10; i++)
                {
                    Assert.AreEqual(teams[i].Id, existingTeams[i].Id);
                    Assert.AreEqual(teams[i].Name, existingTeams[i].Name);
                }
            }
        }

        [TestMethod]
        public void SelectNotExistingFromTableWithUserGeneratedGuidAsPrimaryKeyShouldWork()
        {
            using (var db = new UserGeneratedTeamContext())
            {
                var teams = new List<Team>();

                // Add ten teams to the database (Team 0 - Team 9)
                for (int i = 0; i < 10; i++)
                {
                    teams.Add(new Team { Id = Guid.NewGuid(), Name = $"Team #{i}" });
                }

                // Save the ten first teams to the database.
                db.BulkInsertAll(new BulkInsertRequest<Team>
                {
                    Entities = teams
                });

                // Add another ten teams (Team 10 - Team 19) to
                // the list but not to the database.
                for (int i = 10; i < 20; i++)
                {
                    teams.Add(new Team { Id = Guid.NewGuid(), Name = $"Team #{i}" });
                }

                // The only teams we should get back out of the 20 teams (Team 0 - Team 19)
                // are the last ten that we did not save to the database.
                var existingTeams = db.BulkSelectNotExisting<Team, Team>(new BulkSelectRequest<Team>(new[] { "Id" }, teams));
                Assert.AreEqual(10, existingTeams.Count);
                for (int i = 0; i < 10; i++)
                {
                    Assert.AreEqual(teams[i + 10].Id, existingTeams[i].Id);
                    Assert.AreEqual(teams[i + 10].Name, existingTeams[i].Name);
                }
            }
        }

        [TestMethod]
        public void SelectExistingFromTableWithDbGeneratedGuidAsPrimaryKeyShouldWork()
        {
            using (var db = new DbGeneratedTeamContext())
            {
                var teams = new List<DM.Teams.UsingDbGeneratedGuidKeys.Team>();

                // Add ten teams to the database (Team 0 - Team 9)
                for (int i = 0; i < 10; i++)
                {
                    teams.Add(new DM.Teams.UsingDbGeneratedGuidKeys.Team { Name = $"Team #{i}" });
                }

                // Save the ten first teams to the database.
                db.BulkInsertAll(new BulkInsertRequest<DM.Teams.UsingDbGeneratedGuidKeys.Team>
                {
                    Entities = teams
                });

                // Add another ten teams (Team 10 - Team 19) to
                // the list but not to the database.
                for (int i = 10; i < 20; i++)
                {
                    teams.Add(new DM.Teams.UsingDbGeneratedGuidKeys.Team { Name = $"Team #{i}" });
                }

                // The only teams we should get back out of the 20 teams (Team 0 - Team 19)
                // are the first ten that we saved to the database.
                var existingTeams = db.BulkSelectExisting<DM.Teams.UsingDbGeneratedGuidKeys.Team, DM.Teams.UsingDbGeneratedGuidKeys.Team>(new BulkSelectRequest<DM.Teams.UsingDbGeneratedGuidKeys.Team>(new[] { "Id" }, teams));
                Assert.AreEqual(10, existingTeams.Count);
                for (int i = 0; i < 10; i++)
                {
                    Assert.AreEqual(teams[i].Id, existingTeams[i].Id);
                    Assert.AreEqual(teams[i].Name, existingTeams[i].Name);
                }
            }
        }

        [TestMethod]
        public void SelectNotExistingFromTableWithDbGeneratedGuidAsPrimaryKeyShouldWork()
        {
            using (var db = new DbGeneratedTeamContext())
            {
                var teams = new List<DM.Teams.UsingDbGeneratedGuidKeys.Team>();

                // Add ten teams to the database (Team 0 - Team 9)
                for (int i = 0; i < 10; i++)
                {
                    teams.Add(new DM.Teams.UsingDbGeneratedGuidKeys.Team { Name = $"Team #{i}" });
                }

                // Save the ten first teams to the database.
                db.BulkInsertAll(new BulkInsertRequest<DM.Teams.UsingDbGeneratedGuidKeys.Team>
                {
                    Entities = teams
                });

                // Add another ten teams (Team 10 - Team 19) to
                // the list but not to the database.
                for (int i = 10; i < 20; i++)
                {
                    teams.Add(new DM.Teams.UsingDbGeneratedGuidKeys.Team { Name = $"Team #{i}" });
                }

                // The only teams we should get back out of the 20 teams (Team 0 - Team 19)
                // are the last ten that we did not save to the database.
                var existingTeams = db.BulkSelectNotExisting<DM.Teams.UsingDbGeneratedGuidKeys.Team, DM.Teams.UsingDbGeneratedGuidKeys.Team>(new BulkSelectRequest<DM.Teams.UsingDbGeneratedGuidKeys.Team>(new[] { "Id" }, teams));
                Assert.AreEqual(10, existingTeams.Count);
                for (int i = 0; i < 10; i++)
                {
                    Assert.AreEqual(teams[i + 10].Id, existingTeams[i].Id);
                    Assert.AreEqual(teams[i + 10].Name, existingTeams[i].Name);
                }
            }
        }

        [TestMethod]
        public void PrimitiveTypeValuesMatchingExistingEntitiesShouldBeSelected()
        {
            using (var db = new NumberContext())
            {
                var now = DateTime.Now;

                // Save 200 numbers (1 to 200) to the database.
                var numbers = GenerateNumbers(1, 200, now).ToArray();
                db.BulkInsertAll(new BulkInsertRequest<Number>
                {
                    Entities = numbers,
                    EnableRecursiveInsert = EnableRecursiveInsert.Yes
                });

                // Create a list of 100 numbers with values 151 to 250
                var nums = GenerateNumbers(151, 100, now)
                    .Select(n => n.Value)
                    .ToList();

                // Numbers 151 to 200 out of 151 to 250 should be selected.
                var existingNumbers = db.BulkSelectExisting<long, Number>(new BulkSelectRequest<long>
                {
                    Items = nums,
                    KeyPropertyMappings = new[]
                    {
                        new KeyPropertyMapping
                        {
                            ItemPropertyName = null,
                            EntityPropertyName = "Value"
                        },
                    }
                }).ToArray();

                Assert.AreEqual(50, existingNumbers.Length);
                for (int i = 0; i < 50; i++)
                {
                    Assert.AreEqual(nums[i], existingNumbers[i]);
                }
            }
        }

        [TestMethod]
        public void EntitiesOfDifferentTypeMatchingExistingEntitiesShouldBeSelected()
        {
            using (var db = new NumberContext())
            {
                var now = DateTime.Now;

                // Save 200 numbers (1 to 200) to the database.
                var numbers = GenerateNumbers(1, 200, now).ToArray();
                db.BulkInsertAll(new BulkInsertRequest<Number>
                {
                    Entities = numbers,
                    EnableRecursiveInsert = EnableRecursiveInsert.Yes
                });

                // Create a list of 100 numbers with values 151 to 250
                var nums = GenerateNumbers(151, 100, now)
                    .Select(n => new Num { Val = n.Value })
                    .ToList();

                // Numbers 151 to 200 out of 151 to 250 should be selected.
                var existingNumbers = db.BulkSelectExisting<Num, Number>(new BulkSelectRequest<Num>
                {
                    Items = nums,
                    KeyPropertyMappings = new[]
                    {
                        new KeyPropertyMapping
                        {
                            ItemPropertyName = "Val",
                            EntityPropertyName = "Value"
                        },
                    }
                }).ToArray();

                Assert.AreEqual(50, existingNumbers.Length);
                for (int i = 0; i < 50; i++)
                {
                    Assert.AreEqual(nums[i], existingNumbers[i]);
                }
            }
        }

        [TestMethod]
        public void EntitiesOfSameTypeMatchingExistingEntitiesShouldBeSelected()
        {
            using (var db = new NumberContext())
            {
                var now = DateTime.Now;

                // Save 200 numbers (1 to 200) to the database.
                var numbers = GenerateNumbers(1, 200, now).ToArray();
                db.BulkInsertAll(new BulkInsertRequest<Number>
                {
                    Entities = numbers,
                    EnableRecursiveInsert = EnableRecursiveInsert.Yes
                });

                // Create a list of 100 numbers with values 151 to 250
                numbers = GenerateNumbers(151, 100, now).ToArray();

                // Numbers 151 to 200 out of 151 to 250 should be selected.
                var existingNumbers = db.BulkSelectExisting<Number, Number>(new BulkSelectRequest<Number>
                {
                    Items = numbers,
                    KeyPropertyMappings = new[]
                    {
                        new KeyPropertyMapping
                        {
                            ItemPropertyName = "Value",
                            EntityPropertyName = "Value"
                        },
                    }
                }).ToArray();

                Assert.AreEqual(50, existingNumbers.Length);
                for (int i = 0; i < 50; i++)
                {
                    Assert.AreSame(numbers[i], existingNumbers[i]);
                }
            }
        }

        [TestMethod]
        public void ExistingEntitiesShouldBeSelectedUsingRuntimeTypes()
        {
            using (var db = new NumberContext())
            {
                var now = DateTime.Now;

                // Save 200 numbers (1 to 200) to the database.
                var numbers = GenerateNumbers(1, 200, now).ToArray();
                db.BulkInsertAll(new BulkInsertRequest<Number>
                {
                    Entities = numbers,
                    EnableRecursiveInsert = EnableRecursiveInsert.Yes
                });

                // Create a list of 100 numbers with values 151 to 250
                numbers = GenerateNumbers(151, 100, now).ToArray();

                // Numbers 151 to 200 out of 151 to 250 should be selected.
                var request = typeof(BulkSelectRequest<>).MakeGenericType(typeof(Number));
                var r = Activator.CreateInstance(request, new[] { "Value" }, numbers.ToList(), null);

                Type ex = typeof(DbContextExtensions);
                MethodInfo mi = ex.GetMethod("BulkSelectExisting");
                MethodInfo miGeneric = mi.MakeGenericMethod(new[] { typeof(Number), typeof(Number) });
                object[] args = { db, r };
                var existingNumbers = (List<Number>)miGeneric.Invoke(null, args);

                Assert.AreEqual(50, existingNumbers.Count);
                for (int i = 0; i < 50; i++)
                {
                    Assert.AreSame(numbers[i], existingNumbers[i]);
                }
            }
        }

        [TestMethod]
        public void NonExistingEntitiesWithDifferentColumnNameShouldBeSelected()
        {
            using (var db = new NumberContext())
            {
                // Save 200 numbers (1 to 200) to the database.
                var firstParity = new Parity() { Name = "Parity#1", UpdatedAt = DateTime.Now, UpdatedBy = "Me" };
                var secondParity = new Parity() { Name = "Parity#2", UpdatedAt = DateTime.Now, UpdatedBy = "Me" };
                db.BulkInsertAll(new BulkInsertRequest<Parity>
                {
                    Entities = new List<Parity> { firstParity, secondParity },
                    EnableRecursiveInsert = EnableRecursiveInsert.Yes
                });

                var thirdParyity = new Parity() { Name = "Parity#3", UpdatedAt = DateTime.Now, UpdatedBy = "Me" };

                var parities = new List<Parity>();
                parities.AddRange(db.Parities.ToArray());
                parities.Add(thirdParyity);

                List<Parity> nonExistingParity = db.BulkSelectNotExisting<Parity, Parity>(new BulkSelectRequest<Parity>()
                {
                    Items = new List<Parity> { firstParity, secondParity, thirdParyity },
                    KeyPropertyMappings = new[]
                    {
                        new KeyPropertyMapping
                        {
                            ItemPropertyName = "Id",
                            EntityPropertyName = "Id"
                        },
                    }
                }).ToList();

                Assert.AreEqual(1, nonExistingParity.Count);
            }
        }
    }
}