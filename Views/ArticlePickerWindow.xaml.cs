using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using FacturationMercuriale.Models;
using FacturationMercuriale.Services;

namespace FacturationMercuriale.Views
{
    public partial class ArticlePickerWindow : Window
    {
        private readonly ArticlePickerViewModel _vm;
        public event EventHandler<Article?>? ArticleChosen;

        public ArticlePickerWindow(MercurialeService mercuriale)
        {
            InitializeComponent();
            _vm = new ArticlePickerViewModel(mercuriale);
            DataContext = _vm;
        }

        private void ArticlesGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            AjouterArticleSelectionne();
        }

        private void ArticlesGrid_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && _vm.SelectedArticle != null)
            {
                AjouterArticleSelectionne();
                e.Handled = true;
            }
        }

        private void SearchBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && _vm.SelectedArticle != null)
            {
                AjouterArticleSelectionne();
                e.Handled = true;
            }
            else if (e.Key == Key.Down && ArticlesGrid.Items.Count > 0)
            {
                ArticlesGrid.Focus();
                if (ArticlesGrid.Items.Count > 0)
                {
                    ArticlesGrid.SelectedIndex = 0;
                    var item = ArticlesGrid.Items[0];
                    ArticlesGrid.ScrollIntoView(item);
                }
                e.Handled = true;
            }
        }

        private void ComboBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && _vm.SelectedArticle != null)
            {
                AjouterArticleSelectionne();
                e.Handled = true;
            }
        }

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && Keyboard.Modifiers == ModifierKeys.Control && _vm.SelectedArticle != null)
            {
                AjouterArticleSelectionne();
                e.Handled = true;
            }
            else if (e.Key == Key.Escape)
            {
                Close();
                e.Handled = true;
            }
        }

        private void AjouterArticleSelectionne()
        {
            if (_vm.SelectedArticle != null)
            {
                ArticleChosen?.Invoke(this, _vm.SelectedArticle);
            }
            else
            {
                MessageBox.Show("Veuillez sélectionner un article.", "Attention",
                              MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void ResetFilters_Click(object sender, RoutedEventArgs e)
        {
            _vm.SearchQuery = string.Empty;
            _vm.SelectedRubrique = null;
        }
    }

    internal sealed class ArticlePickerViewModel : INotifyPropertyChanged
    {
        private readonly List<Article> _allArticles;
        public ObservableCollection<Rubrique> Rubriques { get; } = new();
        public ObservableCollection<Article> FilteredArticles { get; } = new();

        private Rubrique? _selectedRubrique;
        public Rubrique? SelectedRubrique
        {
            get => _selectedRubrique;
            set { if (Set(ref _selectedRubrique, value)) { SelectedSousRubrique = null; Refresh(); } }
        }

        private SousRubrique? _selectedSousRubrique;
        public SousRubrique? SelectedSousRubrique
        {
            get => _selectedSousRubrique;
            set { if (Set(ref _selectedSousRubrique, value)) Refresh(); }
        }

        private string _searchQuery = string.Empty;
        public string SearchQuery
        {
            get => _searchQuery;
            set { if (Set(ref _searchQuery, value)) Refresh(); }
        }

        private Article? _selectedArticle;
        public Article? SelectedArticle
        {
            get => _selectedArticle;
            set => Set(ref _selectedArticle, value);
        }

        public ArticlePickerViewModel(MercurialeService mercuriale)
        {
            _allArticles = mercuriale.Rubriques
                .SelectMany(r => r.SousRubriques)
                .SelectMany(sr => sr.Articles)
                .ToList();

            foreach (var r in mercuriale.Rubriques)
                Rubriques.Add(r);

            Refresh();
        }

        private void Refresh()
        {
            FilteredArticles.Clear();
            string q = (SearchQuery ?? "").Trim().ToLowerInvariant();

            IEnumerable<Article> items;

            if (!string.IsNullOrWhiteSpace(q))
            {
                items = _allArticles.Where(a =>
                    (a.RefArticle ?? "").ToLowerInvariant().Contains(q) ||
                    (a.DesignationFr ?? "").ToLowerInvariant().Contains(q));
            }
            else
            {
                items = SelectedSousRubrique?.Articles ??
                        SelectedRubrique?.SousRubriques?.SelectMany(x => x.Articles) ??
                        _allArticles.Take(100);
            }

            foreach (var a in items.Take(800))
                FilteredArticles.Add(a);
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? n = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
        private bool Set<T>(ref T f, T v, [CallerMemberName] string? n = null)
        {
            if (Equals(f, v)) return false;
            f = v;
            OnPropertyChanged(n);
            return true;
        }
    }
}