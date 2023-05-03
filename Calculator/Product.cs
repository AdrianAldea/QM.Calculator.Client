using System;
using System.ComponentModel;

namespace Calculator {
    public class Product : INotifyPropertyChanged {
        public int Id {
            get; set;
        }
        public string DistributionCompany { get; set; }
        public string Name {
            get; set;
        } = string.Empty;
        public string Type { get; set; }
        public DateTime CreatedDate { get; set; }
        private double quantity;

        public double Quantity {
            get { return quantity; }
            set {
                quantity = value;
                OnPropertyChanged("Quantity");
            }
        }

        private double price;

        public double Price {
            get { return price; }
            set {
                price = value;
                OnPropertyChanged("Price");
            }
        }
        private double total;

        public double Total {
            get { return total; }
            set {
                total = value;
                OnPropertyChanged("Total");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged(string strCaller = null) {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(strCaller));
        }
    }
}
