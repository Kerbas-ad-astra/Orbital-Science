﻿#region license
/* DMagic Orbital Science - DMContractGenerators
 * Static utilities class for generating contract parameters
 *
 * Copyright (c) 2014, David Grandy <david.grandy@gmail.com>
 * All rights reserved.
 * 
 * Redistribution and use in source and binary forms, with or without modification, 
 * are permitted provided that the following conditions are met:
 * 
 * 1. Redistributions of source code must retain the above copyright notice, 
 * this list of conditions and the following disclaimer.
 * 
 * 2. Redistributions in binary form must reproduce the above copyright notice, 
 * this list of conditions and the following disclaimer in the documentation and/or other materials 
 * provided with the distribution.
 * 
 * 3. Neither the name of the copyright holder nor the names of its contributors may be used 
 * to endorse or promote products derived from this software without specific prior written permission.
 * 
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, 
 * INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE 
 * DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, 
 * SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE 
 * GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF 
 * LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT 
 * OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 *  
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using Contracts;
using DMagic.Contracts;
using DMagic.Parameters;

namespace DMagic
{
	static class DMCollectContractGenerator
	{
		private static System.Random rand = DMUtils.rand;

		//Use for magnetic field survey
		internal static DMCollectScience fetchScienceContract(CelestialBody Body, ExperimentSituations Situation, DMScienceContainer DMScience)
		{
			AvailablePart aPart;
			string name;

			//Choose science container based on a given science experiment
			name = DMUtils.availableScience["All"].FirstOrDefault(n => n.Value == DMScience).Key;
			DMUtils.DebugLog("Checking Contract Requirements");

			if (DMScience.Exp == null)
				return null;

			//Determine if the science part is available if applicable
			if (DMScience.SciPart != "None")
			{
				DMUtils.DebugLog("Checking For Part {0} Now", DMScience.SciPart);
				aPart = PartLoader.getPartInfoByName(DMScience.SciPart);
				if (aPart == null)
					return null;
				if (!ResearchAndDevelopment.PartModelPurchased(aPart))
					return null;
				DMUtils.DebugLog("Part: [{0}] Purchased; Contract Meets Requirements", aPart.name);
			}

			return new DMCollectScience(Body, Situation, "", name, 1);
		}
	}

	static class DMSurveyGenerator
	{
		private static System.Random rand = DMUtils.rand;

		//Used for initial orbital and surface survey parameter
		internal static DMCollectScience fetchSurveyScience(Contract.ContractPrestige c, List<CelestialBody> cR, List<CelestialBody> cUR, DMScienceContainer DMScience)
		{
			CelestialBody body;
			ExperimentSituations targetSituation;
			ScienceSubject sub;
			AvailablePart aPart;
			string name;
			string biome = "";

			name = DMUtils.availableScience["All"].FirstOrDefault(n => n.Value == DMScience).Key;

			//Determine if the science part is available if applicable
			if (DMScience.SciPart != "None")
			{
				DMUtils.DebugLog("Checking For Part {0} Now", DMScience.SciPart);
				aPart = PartLoader.getPartInfoByName(DMScience.SciPart);
				if (aPart == null)
					return null;
				if (!ResearchAndDevelopment.PartModelPurchased(aPart))
					return null;
				DMUtils.DebugLog("Part: [{0}] Purchased; Contract Meets Requirements", aPart.name);
			}

			body = DMUtils.nextTargetBody(c, cR, cUR);
			DMUtils.DebugLog("Body: {0} Selected", body.name);
			if (body == null)
				return null;

			//Make sure our experiment is OK
			if (DMScience.Exp == null)
				return null;

			if (!body.atmosphere && DMScience.Exp.requireAtmosphere)
				return null;
			if (((ExperimentSituations)DMScience.SitMask & ExperimentSituations.InSpaceHigh) == ExperimentSituations.InSpaceHigh && ((ExperimentSituations)DMScience.SitMask & ExperimentSituations.InSpaceLow) == ExperimentSituations.InSpaceLow)
			{
				if (rand.Next(0, 2) == 0)
					targetSituation = ExperimentSituations.InSpaceHigh;
				else
					targetSituation = ExperimentSituations.InSpaceLow;
			}
			else if (((ExperimentSituations)DMScience.SitMask & ExperimentSituations.InSpaceHigh) == ExperimentSituations.InSpaceHigh)
				targetSituation = ExperimentSituations.InSpaceHigh;
			else
				targetSituation = ExperimentSituations.InSpaceLow;

			DMUtils.DebugLog("Experimental Situation: {0} Selected", targetSituation.ToString());

			if (DMUtils.biomeRelevant(targetSituation, DMScience.BioMask) && targetSituation != ExperimentSituations.SrfSplashed)
			{
				DMUtils.DebugLog("Checking For Biome Usage");
				List<string> bList = DMUtils.fetchBiome(body, DMScience.Exp, targetSituation);
				if (bList.Count == 0)
				{
					DMUtils.DebugLog("Planet All Tapped Out; No Remaining Science Here");
					return null;
				}
				else
				{
					biome = bList[rand.Next(0, bList.Count)];
					DMUtils.DebugLog("Acceptable Biome Found: {0}", biome);
				}
			}

			DMUtils.DebugLog("Checking For Remaining Science");
			//Make sure that our chosen science subject has science remaining to be gathered
			if ((sub = ResearchAndDevelopment.GetSubjectByID(string.Format("{0}@{1}{2}{3}", DMScience.Exp.id, body.name, targetSituation, biome.Replace(" ", "")))) != null)
			{
				if (sub.scientificValue < 0.5f)
					return null;
			}

			return new DMCollectScience(body, targetSituation, "", name, 0);
		}

		//Used for orbital survey
		internal static DMCollectScience fetchSurveyScience(CelestialBody Body, DMScienceContainer DMScience)
		{
			ExperimentSituations targetSituation;
			ScienceSubject sub;
			AvailablePart aPart;
			string name;

			name = DMUtils.availableScience["All"].FirstOrDefault(n => n.Value == DMScience).Key;

			//Determine if the science part is available if applicable
			if (DMScience.SciPart != "None")
			{
				DMUtils.DebugLog("Checking For Part {0} Now", DMScience.SciPart);
				aPart = PartLoader.getPartInfoByName(DMScience.SciPart);
				if (aPart == null)
					return null;
				if (!ResearchAndDevelopment.PartModelPurchased(aPart))
					return null;
				DMUtils.DebugLog("Part: [{0}] Purchased; Contract Meets Requirements", aPart.name);
			}

			//Make sure our experiment is OK
			if (DMScience.Exp == null)
				return null;

			if (!Body.atmosphere && DMScience.Exp.requireAtmosphere)
				return null;
			if (((ExperimentSituations)DMScience.SitMask & ExperimentSituations.InSpaceHigh) == ExperimentSituations.InSpaceHigh && ((ExperimentSituations)DMScience.SitMask & ExperimentSituations.InSpaceLow) == ExperimentSituations.InSpaceLow)
			{
				if (rand.Next(0, 2) == 0)
					targetSituation = ExperimentSituations.InSpaceHigh;
				else
					targetSituation = ExperimentSituations.InSpaceLow;
			}
			else if (((ExperimentSituations)DMScience.SitMask & ExperimentSituations.InSpaceHigh) == ExperimentSituations.InSpaceHigh)
				targetSituation = ExperimentSituations.InSpaceHigh;
			else
				targetSituation = ExperimentSituations.InSpaceLow;

			if (DMUtils.biomeRelevant(targetSituation, DMScience.BioMask))
			{
				DMUtils.DebugLog("Checking For Biome Usage");
				List<string> bList = DMUtils.fetchBiome(Body, DMScience.Exp, targetSituation);
				if (bList.Count == 0)
				{
					DMUtils.DebugLog("Planet All Tapped Out; No Remaining Science Here");
					return null;
				}
			}

			if ((sub = ResearchAndDevelopment.GetSubjectByID(string.Format("{0}@{1}{2}", DMScience.Exp.id, Body.name, targetSituation))) != null)
				if (sub.scientificValue < 0.5f)
					return null;

			return new DMCollectScience(Body, targetSituation, "", name, 0);
		}
	}

	static class DMAsteroidGenerator
	{
		private static System.Random rand = DMUtils.rand;

		internal static DMAsteroidParameter fetchAsteroidParameter(int Size, DMScienceContainer DMScience)
		{
			ExperimentSituations targetSituation;
			AvailablePart aPart;
			string name;

			name = DMUtils.availableScience["All"].FirstOrDefault(n => n.Value == DMScience).Key;

			//Determine if the science part is available if applicable
			if (DMScience.SciPart != "None")
			{
				DMUtils.DebugLog("Checking For Part {0} Now", DMScience.SciPart);
				aPart = PartLoader.getPartInfoByName(DMScience.SciPart);
				if (aPart == null)
					return null;
				if (!ResearchAndDevelopment.PartModelPurchased(aPart))
					return null;
				DMUtils.DebugLog("Part: [{0}] Purchased; Contract Meets Requirements", aPart.name);
			}

			if (DMScience.Exp == null)
				return null;

			if (((ExperimentSituations)DMScience.SitMask & ExperimentSituations.InSpaceLow) == ExperimentSituations.InSpaceLow)
				if (((ExperimentSituations)DMScience.SitMask & ExperimentSituations.SrfLanded) == ExperimentSituations.SrfLanded)
					if (rand.Next(0, 2) == 0)
						targetSituation = ExperimentSituations.SrfLanded;
					else
						targetSituation = ExperimentSituations.InSpaceLow;
				else
					targetSituation = ExperimentSituations.InSpaceLow;
			else if (((ExperimentSituations)DMScience.SitMask & ExperimentSituations.SrfLanded) == ExperimentSituations.SrfLanded)
				targetSituation = ExperimentSituations.SrfLanded;
			else
				return null;

			DMUtils.DebugLog("Successfully Generated Asteroid Survey Parameter");
			return new DMAsteroidParameter(Size, targetSituation, name);
		}

	}

	static class DMAnomalyGenerator
	{
		private static System.Random rand = DMUtils.rand;

		internal static DMCollectScience fetchAnomalyParameter(CelestialBody Body, DMAnomalyObject City)
		{
			ExperimentSituations targetSituation;
			ScienceSubject sub;
			string subject, anomName;

			if (Body == null)
				return null;

			if (City == null)
				return null;

			if (ResearchAndDevelopment.GetExperiment("AnomalyScan") == null)
				return null;

			if (rand.Next(0, 2) == 0)
				targetSituation = ExperimentSituations.SrfLanded;
			else
				targetSituation = ExperimentSituations.FlyingLow;

			anomName = DMagic.Part_Modules.DMAnomalyScanner.anomalyCleanup(City.Name);

			subject = string.Format("AnomalyScan@{0}{1}{2}", Body.name, targetSituation, anomName);

			//Make sure that our chosen science subject has science remaining to be gathered
			if ((sub = ResearchAndDevelopment.GetSubjectByID(subject)) != null)
			{
				if (sub.scientificValue < 0.4f)
					return null;
			}

			DMUtils.DebugLog("Primary Anomaly Parameter Assigned");
			return new DMCollectScience(Body, targetSituation, anomName, "Anomaly Scan", 2);
		}

		internal static DMAnomalyParameter fetchAnomalyParameter(CelestialBody Body, DMAnomalyObject City, DMScienceContainer DMScience)
		{
			AvailablePart aPart;
			ExperimentSituations targetSituation;
			List<ExperimentSituations> situations;
			string name;

			name = DMUtils.availableScience["All"].FirstOrDefault(n => n.Value == DMScience).Key;

			if (DMScience.Exp == null)
				return null;

			//Determine if the science part is available if applicable
			if (DMScience.SciPart != "None")
			{
				DMUtils.DebugLog("Checking For Part {0} Now", DMScience.SciPart);
				aPart = PartLoader.getPartInfoByName(DMScience.SciPart);
				if (aPart == null)
					return null;
				if (!ResearchAndDevelopment.PartModelPurchased(aPart))
					return null;
				DMUtils.DebugLog("Part: [{0}] Purchased; Contract Meets Requirements", aPart.name);
			}

			if ((situations = DMUtils.availableSituationsLimited(DMScience.Exp, DMScience.SitMask, Body)).Count == 0)
				return null;
			else
			{
				DMUtils.DebugLog("Acceptable Situations Found");
				targetSituation = situations[rand.Next(0, situations.Count)];
				DMUtils.DebugLog("Experimental Situation: {0}", targetSituation);
			}

			return new DMAnomalyParameter(Body, City, targetSituation, name);
		}
	}
}
