using Marten.Linq;
using Marten.Linq.Parsing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using Marten;
using Marten.Schema;
using System.Reflection;
using Baseline.Reflection;

namespace GTAIdentity.Models
{
    /// <summary>
    /// A ban is associated with a list of IPs, a social club handle and an account id, all of which may or may not be present.
    /// A ban is temporary if it has a non-null <see cref="TimeSpan"/>, otherwise it is permanent.
    /// However, a ban may be manually expunged for a certain reason, in which case it will no longer be active.
    /// </summary>
    public class Ban
    {
        public Guid Id { get; set; }
        public Guid? AccountId { get; set; }
        public Guid? BannerAccountId { get; set; }
        public HashSet<string> KnownIPs { get; set; }
        public string SocialClubHandle { get; set; }
        public string Reason { get; set; }
        public bool Expunged { get; set; }
        public string ExpungeReason { get; set; }
        public DateTime Start { get; set; } = DateTime.Now;
        public DateTime End { get; set; }

        public bool IsActive()
        {
            return !Expunged && End > DateTime.Now;
        }
       
        public string GetExplanation()
        {
            if (IsActive())
                return $"Ban from {Start} to {End}, reason: \"{Reason}\"";
            if (Expunged)
                return $"Expunged ban for reason \"{ExpungeReason}\", original ban reason was from {Start} to {End} for the reason: \"{Reason}\"";
            return $"Inactive ban from {Start} to {End}, reason: \"{Reason}\"";
        }
        

        public override bool Equals(object obj)
        {
            var ban = obj as Ban;
            if (ban == null)
                return false;
            return ban.Id == Id;
        }
        public override int GetHashCode()
        {
            return (17 * GetType().GetHashCode()) * 23 + Id.GetHashCode();
        }

        public static readonly Expression<Func<Ban, bool>> IsBanActiveExpr = ban => !ban.Expunged && ban.End > DateTime.Now;
 
    }


}
