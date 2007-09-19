using System;
using log4net;
using NHibernate.Engine;
using NHibernate.Impl;
using NHibernate.Persister.Entity;
using NHibernate.Type;
using Status=NHibernate.Impl.Status;

namespace NHibernate.Event.Default
{
	/// <summary> A
	///  convenience base class for listeners that respond to requests to reassociate an entity
	/// to a session ( such as through lock() or update() ). 
	/// </summary>
	[Serializable]
	public class AbstractReassociateEventListener
	{
		private static readonly ILog log = LogManager.GetLogger(typeof(AbstractReassociateEventListener));

		/// <summary> 
		/// Associates a given entity (either transient or associated with another session) to the given session. 
		/// </summary>
		/// <param name="event">The event triggering the re-association </param>
		/// <param name="entity">The entity to be associated </param>
		/// <param name="id">The id of the entity. </param>
		/// <param name="persister">The entity's persister instance. </param>
		/// <returns> An EntityEntry representing the entity within this session. </returns>
		protected internal EntityEntry Reassociate(AbstractEvent @event, object entity, object id, IEntityPersister persister)
		{
			if (log.IsDebugEnabled)
			{
				log.Debug("Reassociating transient instance: " + 
					MessageHelper.InfoString(persister, id, @event.Session.Factory));
			}

			IEventSource source = @event.Session;
			EntityKey key = new EntityKey(id, persister);

			source.CheckUniqueness(key, entity);

			//get a snapshot
			object[] values = persister.GetPropertyValues(entity);
			TypeFactory.DeepCopy(values, persister.PropertyTypes, persister.PropertyUpdateability, values);
			object version = Versioning.GetVersion(values, persister);

			EntityEntry newEntry = source.AddEntity(entity, Status.Loaded, values, key, version, LockMode.None, true, persister, false, true);

			new OnLockVisitor(source, id, entity).Process(entity, persister);

			// TODO: H3 - Property Laziness
			//persister.AfterReassociate(entity, source);

			return newEntry;
		}
	}
}