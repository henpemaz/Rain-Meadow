﻿using System;

namespace RainMeadow
{
    public abstract class PersonaSettingsEntity : OnlineEntity
    {
        internal static int personaID = -100;
        public EntityId target;

        public PersonaSettingsEntity(EntityDefinition entityDefinition) : base(entityDefinition) { }

        internal abstract void ApplyCustomizations(Creature creature, OnlinePhysicalObject oe);

        internal void BindEntity(OnlineEntity target)
        {
            this.target = target.id;
        }

        public abstract class PersonaSettingsState : EntityState
        {
            [OnlineField(nullable:true)]
            private EntityId target;

            protected PersonaSettingsState() { }

            protected PersonaSettingsState(OnlineEntity onlineEntity, uint ts) : base(onlineEntity, ts)
            {
                PersonaSettingsEntity personaSettings = (PersonaSettingsEntity)onlineEntity;
                target = personaSettings.target;
            }

            public override void ReadTo(OnlineEntity onlineEntity)
            {
                PersonaSettingsEntity personaSettings = (PersonaSettingsEntity)onlineEntity;
                personaSettings.target = target;
            }
        }
    }
}