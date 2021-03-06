﻿using System;
using System.Windows;
using System.Windows.Media;

namespace WPFColorPickerLib
{
    /// <summary>
    /// Holds a ColorPicker control, and exposes the ColorPicker SelectedColor.
    /// 
    /// Enhanced by Mark Treadwell (1/2/10) to include:
    ///  - Added ability to set ColorPicker initial color via constructor or property
    ///  - Use of Button's IsDefault and IsCancel properties
    ///  - Setting tab behavior
    /// </summary>
    public partial class ColorDialog : Window
    {
        #region Constructors

        /// <summary>
        /// Default constructor initializes to Black.
        /// </summary>
        public ColorDialog()
          : this(Colors.Black, 5)
        { }

        /// <summary>
        /// Constructor with an initial color.
        /// </summary>
        /// <param name="initialColor">Color to set the ColorPicker to.</param>
        public ColorDialog(Color initialColor, Double initialSize)
        {
            InitializeComponent();
            colorPicker.InitialColor = initialColor;
            colorPicker.InitialSize = initialSize;
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// Gets/sets the ColorDialog color.
        /// </summary>
        public Color SelectedColor
        {
            get { return colorPicker.SelectedColor; }
            set { colorPicker.InitialColor = value; }
        }

        /// <summary>
        /// Gets/sets the ColorDialog size.
        /// </summary>
        public Double SelectedSize
        {
            get { return colorPicker.SelectedSize; }
            set { colorPicker.InitialSize = value; }
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// Close ColorDialog, accepting color selection.
        /// </summary>
        private void btnOK_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }

        /// <summary>
        ///  Close ColorDialog, rejecting color selection.
        /// </summary>
        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        #endregion
    }
}
