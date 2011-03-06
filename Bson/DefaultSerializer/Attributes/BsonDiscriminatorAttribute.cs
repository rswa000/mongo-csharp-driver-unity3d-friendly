﻿/* Copyright 2010-2011 10gen Inc.
*
* Licensed under the Apache License, Version 2.0 (the "License");
* you may not use this file except in compliance with the License.
* You may obtain a copy of the License at
*
* http://www.apache.org/licenses/LICENSE-2.0
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
using System.Text;

namespace MongoDB.Bson.DefaultSerializer {
    /// <summary>
    /// Specifies the discriminator and related options for a class.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
    public class BsonDiscriminatorAttribute : Attribute {
        #region private fields
        private string discriminator;
        private bool required;
        private bool rootClass;
        #endregion

        #region constructors
        /// <summary>
        /// Initializes a new instance of the BsonDiscriminatorAttribute class.
        /// </summary>
        public BsonDiscriminatorAttribute() {
        }

        /// <summary>
        /// Initializes a new instance of the BsonDiscriminatorAttribute class.
        /// </summary>
        /// <param name="discriminator">The discriminator.</param>
        public BsonDiscriminatorAttribute(
            string discriminator
        ) {
            this.discriminator = discriminator;
        }
        #endregion

        #region public properties
        /// <summary>
        /// Gets the discriminator.
        /// </summary>
        public string Discriminator {
            get { return discriminator; }
        }

        /// <summary>
        /// Gets or sets whether the discriminator is required.
        /// </summary>
        public bool Required {
            get { return required; }
            set { required = value; }
        }

        /// <summary>
        /// Gets or sets whether this is a root class.
        /// </summary>
        public bool RootClass {
            get { return rootClass; }
            set { rootClass = value; }
        }
        #endregion
    }
}
