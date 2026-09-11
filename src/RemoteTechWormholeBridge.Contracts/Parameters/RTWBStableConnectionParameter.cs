using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ContractConfigurator;
using ContractConfigurator.Parameters;
using Contracts;
using KSP.Localization;
using RemoteTechWormholeBridge.API;
using RemoteTechWormholeBridge.Core.Contracts;
using UnityEngine;

namespace RemoteTechWormholeBridge.Contracts.Parameters
{
    public sealed class RTWBStableConnectionFactory : ParameterFactory
    {
        private string pairId;
        private string remoteMouthId;

        public override bool Load(ConfigNode configNode)
        {
            bool valid = base.Load(configNode);
            valid &= ConfigNodeUtil.ParseValue<string>(configNode, "pairId", value => pairId = value, this);
            valid &= ConfigNodeUtil.ParseValue<string>(configNode, "remoteMouthId", value => remoteMouthId = value, this);
            return valid;
        }

        public override ContractParameter Generate(Contract contract)
        {
            double duration = ContractRewardPolicy.StableDurationDays * KSPUtil.dateTimeFormatter.Day;
            return new RTWBStableConnectionParameter(pairId, remoteMouthId, duration, title);
        }
    }

    public sealed class RTWBStableConnectionParameter : ContractConfiguratorParameter
    {
        private string pairId;
        private string remoteMouthId;
        private double duration;
        private ContinuousServiceTimer timer;
        private float nextCheck;
        private bool stateObserved;
        private bool lastLinked;
        private bool lastConnected;

        public RTWBStableConnectionParameter()
        {
            duration = ContractRewardPolicy.StableDurationDays * KSPUtil.dateTimeFormatter.Day;
            timer = new ContinuousServiceTimer(duration);
        }

        internal RTWBStableConnectionParameter(
            string pairId,
            string remoteMouthId,
            double duration,
            string title)
            : base(title)
        {
            this.pairId = pairId;
            this.remoteMouthId = remoteMouthId;
            this.duration = duration;
            timer = new ContinuousServiceTimer(duration);
        }

        protected override string GetParameterTitle()
        {
            if (timer != null && timer.IsRunning)
            {
                double remaining = Math.Max(0, duration - timer.Elapsed(Planetarium.GetUniversalTime()));
                return Localizer.Format("#LOC_RTWB_contract_stable_remaining", DurationUtil.StringValue(remaining));
            }
            return base.GetParameterTitle();
        }

        protected override void OnUpdate()
        {
            base.OnUpdate();
            if (state == ParameterState.Complete || Time.realtimeSinceStartup < nextCheck)
                return;
            nextCheck = Time.realtimeSinceStartup + 1f;
            if (!RTWBAPI.RuntimeStateAvailable)
                return;

            IReadOnlyList<RTWBActiveLinkSnapshot> links = RTWBAPI.GetActiveLinks(pairId);
            bool linked = links.Count != 0;
            List<ContractActiveLinkState> contractLinks = links.Select(link =>
                new ContractActiveLinkState(
                    link.MouthAId,
                    link.VesselAId.ToString("D"),
                    link.MouthBId,
                    link.VesselBId.ToString("D")))
                .ToList();
            var connectedVessels = new HashSet<string>(ConnectedRemoteEndpoints(links)
                .Where(HasKscConnection)
                .Select(id => id.ToString("D")), StringComparer.Ordinal);
            bool connected = ContractLinkEvaluator.HasRemoteKscService(
                contractLinks, remoteMouthId, connectedVessels);
            LogStateChanges(linked, connected);

            double now = Planetarium.GetUniversalTime();
            ServiceTimerTransition transition = timer.Update(now, linked && connected);
            if (transition == ServiceTimerTransition.Started)
                ContractsLog.Info("stability-start pairId=" + pairId + " ut=" +
                                  now.ToString("R", CultureInfo.InvariantCulture));
            else if (transition == ServiceTimerTransition.Reset)
                ContractsLog.Info("stability-reset pairId=" + pairId + " ut=" +
                                  now.ToString("R", CultureInfo.InvariantCulture));
            else if (transition == ServiceTimerTransition.Completed)
            {
                ContractsLog.Info("stability-complete pairId=" + pairId + " ut=" +
                                  now.ToString("R", CultureInfo.InvariantCulture));
                SetState(ParameterState.Complete);
            }

            if (timer.IsRunning)
                GetTitle();
        }

        protected override void OnParameterSave(ConfigNode node)
        {
            node.AddValue("pairId", pairId);
            node.AddValue("remoteMouthId", remoteMouthId);
            node.AddValue("duration", duration.ToString("R", CultureInfo.InvariantCulture));
            node.AddValue("startedAt", (timer == null ? Double.NaN : timer.StartedAt)
                .ToString("R", CultureInfo.InvariantCulture));
            node.AddValue("timerComplete", timer != null && timer.IsComplete);
        }

        protected override void OnParameterLoad(ConfigNode node)
        {
            pairId = node.GetValue("pairId");
            remoteMouthId = node.GetValue("remoteMouthId");
            double parsedDuration;
            duration = Double.TryParse(node.GetValue("duration"), NumberStyles.Float,
                CultureInfo.InvariantCulture, out parsedDuration)
                ? parsedDuration
                : ContractRewardPolicy.StableDurationDays * KSPUtil.dateTimeFormatter.Day;
            timer = new ContinuousServiceTimer(duration);

            double startedAt;
            if (!Double.TryParse(node.GetValue("startedAt"), NumberStyles.Float,
                    CultureInfo.InvariantCulture, out startedAt))
                startedAt = Double.NaN;
            bool timerComplete;
            Boolean.TryParse(node.GetValue("timerComplete"), out timerComplete);
            timer.Restore(startedAt, timerComplete);
        }

        private IEnumerable<Guid> ConnectedRemoteEndpoints(IEnumerable<RTWBActiveLinkSnapshot> links)
        {
            return (links ?? Enumerable.Empty<RTWBActiveLinkSnapshot>())
                .Select(link => String.Equals(link.MouthAId, remoteMouthId, StringComparison.Ordinal)
                    ? link.VesselAId
                    : String.Equals(link.MouthBId, remoteMouthId, StringComparison.Ordinal)
                        ? link.VesselBId
                        : Guid.Empty)
                .Where(id => id != Guid.Empty)
                .Distinct();
        }

        private static bool HasKscConnection(Guid vesselId)
        {
            try
            {
                return RemoteTech.API.API.HasConnectionToKSC(vesselId);
            }
            catch (Exception exception)
            {
                ContractsLog.Warning("KSC connectivity check failed vessel=" + vesselId.ToString("D") +
                                     " exception=" + exception.GetType().Name);
                return false;
            }
        }

        private void LogStateChanges(bool linked, bool connected)
        {
            if (!stateObserved || linked != lastLinked)
                ContractsLog.Info("pair-" + (linked ? "linked" : "unlinked") + " pairId=" + pairId);
            if (!stateObserved || connected != lastConnected)
                ContractsLog.Info("gateway-b-ksc-connectivity-" + (connected ? "gained" : "lost") +
                                  " pairId=" + pairId);

            stateObserved = true;
            lastLinked = linked;
            lastConnected = connected;
        }
    }
}
