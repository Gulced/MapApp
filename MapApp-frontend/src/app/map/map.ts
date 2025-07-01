import { Component, OnInit, OnDestroy, NgZone } from '@angular/core';
import { Router } from '@angular/router';
import { ApiService } from '../services/api';
import Map from 'ol/Map';
import View from 'ol/View';
import TileLayer from 'ol/layer/Tile';
import OSM from 'ol/source/OSM';
import VectorLayer from 'ol/layer/Vector';
import VectorSource from 'ol/source/Vector';
import { fromLonLat, transform } from 'ol/proj';
import Draw from 'ol/interaction/Draw';
import { Feature } from 'ol';
import { Geometry, Point } from 'ol/geom';
import { Style, Fill, Stroke, Circle as CircleStyle } from 'ol/style';
import GeoJSON from 'ol/format/GeoJSON';
import WKT from 'ol/format/WKT';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MenuComponent } from '../menu/menu';
import { unByKey } from 'ol/Observable';
import { EventsKey } from 'ol/events';
import Select from 'ol/interaction/Select';
import Modify from 'ol/interaction/Modify';
import { click } from 'ol/events/condition';
import type { FeatureLike } from 'ol/Feature';
import Polygon from 'ol/geom/Polygon';



type EtkilesimModu = 'add-area' | 'edit-area' | 'delete-area' | 'add-point' | 'edit-point' | 'delete-point' | 'none';
type PanelType = 'info' | 'add' | 'edit' | null;
type FeatureType = 'area' | 'point';

@Component({
  selector: 'app-map',
  standalone: true,
  imports: [CommonModule, FormsModule, MenuComponent
    ],
  templateUrl: './map.html',
  styleUrls: ['./map.css']
})
export class MapComponent implements OnInit, OnDestroy {
  map!: Map;
  areaVectorSource!: VectorSource;
  pointVectorSource!: VectorSource;
  private areaLayer!: VectorLayer<any>;
  private pointLayer!: VectorLayer<any>;

  private draw: Draw | null = null;
  private select: Select | null = null;
  private modify: Modify | null = null;
  private haritaTiklamaKey: EventsKey | null = null;

  public etkilesimModu: EtkilesimModu = 'none';
  public sonCizilenFeature: Feature<Geometry> | null = null;
  private cizilenGeoJsonString: string = '';

  public formName: string = '';
  public formDescription: string = '';

  public activePanel: PanelType = null;
  public isDrawingActive: boolean = false;

  public selectedFeatureInfo: { id: string | null, name: string, description: string, type: FeatureType } | null = null;
  private featureToEdit: Feature<Geometry> | null = null;
  private originalGeometryForEdit: Geometry | null = null;
  public selectedFeatureId: string | null = null;

  // -- YENİ: Sağ tık context menu için state
  public contextMenuVisible = false;
  public contextMenuX = 0;
  public contextMenuY = 0;
  public contextMenuFeature: Feature<Geometry> | null = null;

  constructor(
    private apiService: ApiService,
    private router: Router,
    private zone: NgZone
  ) {}

  ngOnInit(): void {
    this.areaVectorSource = new VectorSource({ wrapX: false });
    this.areaLayer = new VectorLayer({ 
      source: this.areaVectorSource, 
      style: feature => this.getFeatureStyle(feature)
    });

    this.pointVectorSource = new VectorSource({ wrapX: false });
    this.pointLayer = new VectorLayer({ 
      source: this.pointVectorSource, 
      style: feature => this.getFeatureStyle(feature)
    });

    this.map = new Map({
      target: 'map',
      layers: [new TileLayer({ source: new OSM() }), this.areaLayer, this.pointLayer],
      view: new View({ center: fromLonLat([35.2433, 38.9637]), zoom: 6 })
    });

    this.loadExistingAreas();
    this.loadExistingPoints();
    window.addEventListener('keydown', this.handleKeyDown.bind(this));

    // Sol tık ile info paneli aç ve seçili alanı vurgula
    this.map.on('click', (event) => {
      if (this.etkilesimModu !== 'none') return;
      // Context menu açıksa kapat ve vurguyu kaldır
      this.contextMenuVisible = false;
      this.contextMenuFeature = null;
      this.selectedFeatureId = null;
      const feature = this.map.forEachFeatureAtPixel(event.pixel, (f) => f as Feature<Geometry>);
      this.zone.run(() => {
        if (feature && feature.get('name')) {
          this.selectedFeatureInfo = {
            id: feature.get('id') ?? null,
            name: feature.get('name'),
            description: feature.get('description'),
            type: feature.get('feature_type') === 'area' ? 'area' : 'point'
          };
          this.selectedFeatureId = feature.get('id') ?? null;
          this.activePanel = 'info';
          this.areaVectorSource.changed();
          this.pointVectorSource.changed();
        } else {
          this.closeActivePanel();
        }
      });
    });

    // -- YENİ: Sağ tık ile context menu aç + VURGULAMA
    this.map.getViewport().addEventListener('contextmenu', (event) => {
      event.preventDefault();
      const pixel = this.map.getEventPixel(event);
      const feature = this.map.forEachFeatureAtPixel(pixel, f => f as Feature<Geometry>);
      this.zone.run(() => {
        if (feature) {
          this.contextMenuVisible = true;
          this.contextMenuX = event.clientX;
          this.contextMenuY = event.clientY;
          this.contextMenuFeature = feature;
          this.selectedFeatureId = feature.get('id'); // VURGULAMA
          this.areaVectorSource.changed();
          this.pointVectorSource.changed();
        } else {
          this.contextMenuVisible = false;
          this.contextMenuFeature = null;
          this.selectedFeatureId = null; // VURGULAMA SIFIRLA
          this.areaVectorSource.changed();
          this.pointVectorSource.changed();
        }
      });
    });

    // -- YENİ: Menü dışında bir yere tıklanınca context menu ve vurguyu kapat
    document.addEventListener('click', this.closeContextMenuOnClick);
  }

  // -- YENİ: Menü dışında tıklamada kapama + vurguyu kaldır
  closeContextMenuOnClick = (event: MouseEvent) => {
    if (this.contextMenuVisible) {
      this.zone.run(() => {
        this.contextMenuVisible = false;
        this.contextMenuFeature = null;
        this.selectedFeatureId = null; // VURGULAMA SIFIRLA
        this.areaVectorSource.changed();
        this.pointVectorSource.changed();
      });
    }
  }

  ngOnDestroy(): void {
    window.removeEventListener('keydown', this.handleKeyDown.bind(this));
    document.removeEventListener('click', this.closeContextMenuOnClick);
    this.tumEtkilesimleriDurdur();
  }

  // -- YENİ: Sağ tık context menü butonları
  onContextEdit(): void {
    if (!this.contextMenuFeature) return;
  
    const feature = this.contextMenuFeature;
    const isArea = feature.get('feature_type') === 'area';
  
    this.selectedFeatureId = feature.get('id');
  
    this.selectedFeatureInfo = {
      id: this.selectedFeatureId,
      name: feature.get('name') || '',
      description: feature.get('description') || '', // MapArea için "note", MapPoint için "description"
      type: isArea ? 'area' : 'point'
    };
  
    this.contextMenuVisible = false;
    this.contextMenuFeature = null;
  
    this.editSelectedFeature(); // Düzenleme moduna geçiş yap
  }
  
  onContextDelete(): void {
    if (!this.contextMenuFeature) return;
  
    const feature = this.contextMenuFeature;
    const isArea = feature.get('feature_type') === 'area';
    const featureId = feature.get('id');
  
    if (!featureId) return;
  
    this.contextMenuVisible = false;
    this.contextMenuFeature = null;
  
    if (confirm('Bu alan/noktayı silmek istediğinize emin misiniz?')) {
      this.baslatSilme(
        isArea ? this.areaVectorSource : this.pointVectorSource,
        isArea ? 'area' : 'point',
        featureId
      );
    }
  }
  

  getFeatureStyle(feature: FeatureLike): Style {
    const isSelected = feature.get('id') === this.selectedFeatureId;
    const featureType = feature.get('feature_type');
  
    if (featureType === 'area') {
      return new Style({
        stroke: new Stroke({
          color: isSelected ? '#FF0000' : '#007bff', // Kırmızı = seçili, mavi = normal
          width: isSelected ? 4 : 2
        }),
        fill: new Fill({
          color: isSelected ? 'rgba(255, 0, 0, 0.1)' : 'rgba(0, 123, 255, 0.2)'
        })
      });
    }
  
    if (featureType === 'point') {
      return new Style({
        image: new CircleStyle({
          radius: isSelected ? 10 : 7,
          fill: new Fill({ color: isSelected ? '#FF0000' : '#28a745' }), // Kırmızı = seçili, yeşil = normal
          stroke: new Stroke({ color: '#fff', width: 2 })
        })
      });
    }
  
    // Bilinmeyen feature tipi için varsayılan stil
    return new Style({
      stroke: new Stroke({ color: '#ccc', width: 1 }),
      fill: new Fill({ color: 'rgba(200, 200, 200, 0.3)' })
    });
  }
  
  getPanelTitle(): string {
    const typeText = this.isAreaMode() ? 'Alan' : 'Nokta';
  
    if (this.activePanel === 'info') {
      return this.selectedFeatureInfo?.name?.trim()
        ? this.selectedFeatureInfo.name
        : `${typeText} Bilgisi`;
    }
  
    if (this.activePanel === 'add') {
      return `Yeni ${typeText} Ekle`;
    }
  
    if (this.activePanel === 'edit') {
      return `${typeText} Düzenle`;
    }
  
    return '';
  }
  

  getPanelClass(): string {
    return typeof this.activePanel === 'string' && this.activePanel.trim()
      ? `${this.activePanel}-panel`
      : '';
  }
  
  public getModDurumuText(): string {
    const modMap: { [key: string]: string } = {
      'none': 'Görüntüleme Modu',
      'add-area': 'Mod: Alan Ekle',
      'edit-area': 'Mod: Alan Düzenle',
      'delete-area': 'Mod: Alan Sil',
      'add-point': 'Mod: Nokta Ekle',
      'edit-point': 'Mod: Nokta Düzenle',
      'delete-point': 'Mod: Nokta Sil'
    };
  
    return modMap[this.etkilesimModu] || 'Görüntüleme Modu';
  }
  
  getDrawingModeText(): string {
    if (typeof this.etkilesimModu !== 'string') return 'Bilinmeyen Mod';
    if (this.etkilesimModu.includes('area')) return 'Alan Çizimi';
    if (this.etkilesimModu.includes('point')) return 'Nokta Ekleme';
    return 'Görüntüleme Modu';
  }
  

  isAreaMode(): boolean {
    if (['info', 'edit'].includes(this.activePanel || '')) {
      return this.selectedFeatureInfo?.type === 'area';
    }
    return typeof this.etkilesimModu === 'string' && this.etkilesimModu.includes('area');
  }
  

  onFeatureSelected(mod: string): void {
    this.etkilesimModunuDegistir(mod as EtkilesimModu);
  }

  etkilesimModunuDegistir(mod: EtkilesimModu): void {
    this.tumEtkilesimleriDurdur();
    this.etkilesimModu = mod;

    if (mod.includes('edit') || mod.includes('delete')) {
      if (mod.includes('area')) {
        this.pointLayer.setVisible(false);
      } else if (mod.includes('point')) {
        this.areaLayer.setVisible(false);
      }
    }

    switch (mod) {
      case 'add-area':    this.baslatCizim('Polygon'); break;
      case 'add-point':   this.baslatCizim('Point'); break;
      case 'edit-area':   this.baslatDuzenleme(this.areaVectorSource); break;
      case 'edit-point':  this.baslatDuzenleme(this.pointVectorSource); break;
      case 'delete-area': this.baslatSilme(this.areaVectorSource, 'area'); break;
      case 'delete-point':this.baslatSilme(this.pointVectorSource, 'point'); break;
    }
  }

  tumEtkilesimleriDurdur(): void {
    this.resetDuzenlemePaneli();
    this.cizimiIptalEt();
    if (this.draw) this.map.removeInteraction(this.draw);
    if (this.select) this.map.removeInteraction(this.select);
    if (this.modify) this.map.removeInteraction(this.modify);
    if (this.haritaTiklamaKey) { unByKey(this.haritaTiklamaKey); this.haritaTiklamaKey = null; }
    if (this.areaLayer) this.areaLayer.setVisible(true);
    if (this.pointLayer) this.pointLayer.setVisible(true);
    this.etkilesimModu = 'none';
    this.isDrawingActive = false;
  }

  closeActivePanel(): void {
    if (this.activePanel === 'add') this.cizimiIptalEt();
    if (this.activePanel === 'edit') this.resetDuzenlemePaneli();
    this.activePanel = null;
    this.selectedFeatureInfo = null;
    this.selectedFeatureId = null;
    this.areaVectorSource?.changed();
    this.pointVectorSource?.changed();
    // Menü panel açıldığında kapansın
    this.contextMenuVisible = false;
    this.contextMenuFeature = null;
  }

  baslatCizim(type: 'Polygon' | 'Point'): void {
    const source: VectorSource<Feature<Geometry>> = type === 'Polygon' 
      ? this.areaVectorSource as VectorSource<Feature<Geometry>> 
      : this.pointVectorSource as VectorSource<Feature<Geometry>>;
  
    this.draw = new Draw({ source, type });
    this.map.addInteraction(this.draw);
    this.isDrawingActive = true;
  
    this.draw.on('drawend', (event: any) => {
      this.zone.run(() => {
        const feature: Feature<Geometry> = event.feature;
        this.sonCizilenFeature = feature;
        feature.set('feature_type', type === 'Polygon' ? 'area' : 'point');
  
        const geoJsonFormat = new GeoJSON();
        this.cizilenGeoJsonString = geoJsonFormat.writeFeature(feature, {
          dataProjection: 'EPSG:4326',
          featureProjection: this.map.getView().getProjection()
        });
  
        if (this.draw) {
          this.map.removeInteraction(this.draw);
        }
  
        this.isDrawingActive = false;
        this.activePanel = 'add';
      });
    });
  }
  geriAlSonNokta(): void {
    if (this.draw) this.draw.removeLastPoint();
  }

  cizimiIptalEt(): void {
    if (this.sonCizilenFeature && !this.sonCizilenFeature.get('id')) {
      const source = this.isAreaMode() ? this.areaVectorSource : this.pointVectorSource;
      source.removeFeature(this.sonCizilenFeature);
    }
    this.sonCizilenFeature = null;
    this.cizilenGeoJsonString = '';
    this.formName = '';
    this.formDescription = '';
  }

  baslatDuzenleme(source: VectorSource): void {
    if (this.select) this.map.removeInteraction(this.select);
    if (this.modify) this.map.removeInteraction(this.modify);

    const editStyle = new Style({
      fill: new Fill({ color: 'rgba(255, 0, 0, 0.3)' }),
      stroke: new Stroke({ color: 'red', width: 3 }),
      image: new CircleStyle({
        radius: 7,
        fill: new Fill({ color: 'rgba(255, 0, 0, 0.3)' }),
        stroke: new Stroke({ color: 'red', width: 2 })
      })
    });

    this.select = new Select({ style: editStyle });
    this.map.addInteraction(this.select);

    this.select.on('select', (event) => {
      this.zone.run(() => {
        if (event.selected.length > 0) {
          const selectedFeature = event.selected[0] as Feature<Geometry>;
          if (source.hasFeature(selectedFeature)) {
            this.featureToEdit = selectedFeature;
            this.originalGeometryForEdit = selectedFeature.getGeometry()?.clone() ?? null;
            this.formName = selectedFeature.get('name') || '';
            this.formDescription = selectedFeature.get('description') || '';
            this.selectedFeatureInfo = {
              id: selectedFeature.get('id') || null,
              name: selectedFeature.get('name'),
              description: selectedFeature.get('description'),
              type: selectedFeature.get('feature_type') === 'area' ? 'area' : 'point'
            };
            this.activePanel = 'edit';
          } else {
            this.select?.getFeatures().clear();
          }
        } else {
          this.closeActivePanel();
        }
      });
    });
  }

  resetDuzenlemePaneli(): void {
    // Eğer düzenlenen bir feature varsa ve orijinal geometrisi kaydedildiyse, eski haline döndür
    if (this.featureToEdit && this.originalGeometryForEdit) {
      this.featureToEdit.setGeometry(this.originalGeometryForEdit);
    }
  
    // Paneli kapat
    this.activePanel = null;
  
    // Seçili feature ve yedeğini temizle
    this.featureToEdit = null;
    this.originalGeometryForEdit = null;
  
    // Seçimi temizle
    this.select?.getFeatures().clear();
  
    // Form alanlarını sıfırla
    this.formName = '';
    this.formDescription = '';
  }
  
  editSelectedFeature(): void {
    if (!this.selectedFeatureId || !this.selectedFeatureInfo) return;
  
    const isArea = this.selectedFeatureInfo.type === 'area';
    const source = isArea ? this.areaVectorSource : this.pointVectorSource;
  
    const feature = source.getFeatures().find(f => f.get('id') === this.selectedFeatureId);
    if (!feature) return;
  
    // Düzenlenecek feature ve orijinal geometri saklanır
    this.featureToEdit = feature;
    this.originalGeometryForEdit = feature.getGeometry()?.clone() ?? null;
  
    // Form alanları doldurulur
    this.formName = feature.get('name') || '';
    this.formDescription = feature.get('description') || '';
  
    // Aktif panel ve tür bilgisi güncellenir
    this.selectedFeatureInfo.type = isArea ? 'area' : 'point';
    this.activePanel = 'edit';
  
    // Context menu kapatılır
    this.contextMenuVisible = false;
    this.contextMenuFeature = null;
  }
  
  deleteSelectedFeature(): void {
    if (!this.selectedFeatureId || !this.selectedFeatureInfo) return;
  
    const isArea = this.selectedFeatureInfo.type === 'area';
    const source = isArea ? this.areaVectorSource : this.pointVectorSource;
  
    const confirmed = confirm(
      `Seçili ${isArea ? 'alanı' : 'noktayı'} silmek istediğinizden emin misiniz?`
    );
    if (!confirmed) return;
  
    this.baslatSilme(source, isArea ? 'area' : 'point', this.selectedFeatureId);
  
    // Seçim ve panel durumu sıfırlanır
    this.selectedFeatureId = null;
    this.selectedFeatureInfo = null;
    this.contextMenuVisible = false;
    this.contextMenuFeature = null;
  }
  

  baslatSilme(source: VectorSource, type: FeatureType, featureIdToDelete?: string): void {
    if (featureIdToDelete) {
      const feature = source.getFeatures().find(f => f.get('id') === featureIdToDelete);
      if (feature) {
        const name = feature.get('name');
        if (confirm(`'${name}' adlı nesneyi silmek istediğinizden emin misiniz?`)) {
          const deleteObservable = type === 'area'
  ? this.apiService.deleteArea(Number(featureIdToDelete))
  : this.apiService.deletePoint(Number(featureIdToDelete));

          deleteObservable.subscribe({
            next: () => this.zone.run(() => { 
              source.removeFeature(feature); 
              this.closeActivePanel();
            }),
            error: () => alert('Nesne silinirken bir hata oluştu.')
          });
        }
      }
      return;
    }
    alert(`${type === 'area' ? 'Alan' : 'Nokta'} silmek için haritadan bir nesne seçin.`);
    this.haritaTiklamaKey = this.map.on('click', (event) => {
      this.map.forEachFeatureAtPixel(event.pixel, (feature) => {
        const typedFeature = feature as Feature<Geometry>;
        if (source.hasFeature(typedFeature)) {
          const featureId = typedFeature.get('id');
          if (featureId && confirm(`'${typedFeature.get('name')}' adlı nesneyi silmek istediğinizden emin misiniz?`)) {
            const deleteObservable = type === 'area' ? this.apiService.deleteArea(featureId) : this.apiService.deletePoint(featureId);
            deleteObservable.subscribe({
              next: () => this.zone.run(() => { 
                source.removeFeature(typedFeature); 
                console.log("Nesne silindi. Silmeye devam edebilirsiniz.");
              }),
              error: () => alert('Nesne silinirken bir hata oluştu.')
            });
          }
          return true;
        }
        return false;
      });
    });
  }
  handleSave(): void {
    if (!this.activePanel) return;
  
    const isArea = this.isAreaMode();
  
    if (this.activePanel === 'add') {
      isArea ? this.kaydetYeniAlan() : this.kaydetYeniNokta();
    } else if (this.activePanel === 'edit') {
      isArea ? this.kaydetAlanDuzenleme() : this.kaydetYeniNokta(); // Assuming you want to call the method for saving a new point
    }
  }
  
  kaydetYeniAlan(): void {
    if (!this.formName || !this.sonCizilenFeature || !this.cizilenGeoJsonString) {
      alert('Alan bilgileri eksik!');
      return;
    }
  
    try {
      // GeoJSON parse et ve coordinates al
      const parsedGeoJson = JSON.parse(this.cizilenGeoJsonString);
      const coordinates = parsedGeoJson.geometry.coordinates; // [[[lng, lat], ...]]
  
      const dto = {
        name: this.formName,
        note: this.formDescription,
        coordinates: coordinates
      };
  
      this.apiService.createArea(dto).subscribe({
        next: (yeniAlan: { id: number }) => this.zone.run(() => {
          this.sonCizilenFeature?.set('id', yeniAlan.id);
          this.sonCizilenFeature?.set('name', this.formName);
          this.sonCizilenFeature?.set('description', this.formDescription);
          this.closeActivePanel();
          this.tumEtkilesimleriDurdur();
        }),
        error: (err) => {
          console.error("❌ Alan kaydedilemedi:", err);
          alert('Alan kaydedilemedi.');
        }
      });
  
    } catch (e) {
      console.error("❌ GeoJSON parse hatası:", e);
      alert('Alan bilgileri okunamadı.');
    }
  }
  
  

  kaydetAlanDuzenleme(): void {
    if (!this.featureToEdit) {
      alert('Düzenlenecek bir alan seçilmedi.');
      return;
    }
  
    if (!this.formName) {
      alert('Lütfen alan adı girin.');
      return;
    }
  
    const featureId = this.featureToEdit.get('id');
    if (!featureId) {
      alert('Geçerli bir alan IDsi bulunamadı.');
      return;
    }
  
    const geometry = this.featureToEdit.getGeometry();
  
    if (!geometry || geometry.getType() !== 'Polygon') {
      alert('Geometri tipi geçersiz veya Polygon değil.');
      return;
    }
  
    // Koordinatları GeoJSON formatına uygun şekilde hazırla
    const coords = (geometry as Polygon).getCoordinates(); // [[[lon, lat], [lon, lat], ...]]
    const formattedCoordinates = coords.map(ring =>
      ring.map(coord => [coord[0], coord[1]])
    );
  
    // Backend'e göndereceğin alan verisi
    const areaData = {
      id: featureId,
      name: this.formName,
      note: this.formDescription,
      coordinates: [formattedCoordinates[0]] // Sadece dış halkayı gönderiyoruz
    };
  
    // API isteği gönder
    this.apiService.updateArea(featureId, areaData).subscribe({
      next: () => {
        // Angular zone içine al, UI güncellemeleri için
        this.zone.run(() => {
          this.featureToEdit?.set('name', this.formName);
          this.featureToEdit?.set('description', this.formDescription);
          this.resetDuzenlemePaneli();
        });
      },
      error: (err) => {
        console.error('Alan güncellenirken hata:', err);
        alert('Alan güncellenemedi.');
      }
    });
  }
  
  kaydetYeniNokta(): void {
    if (!this.formName || !this.sonCizilenFeature) return;
  
    const geometry = this.sonCizilenFeature.getGeometry();
    if (!geometry || geometry.getType() !== 'Point') {
      alert('Geçerli bir nokta çizimi yapılmamış.');
      return;
    }
  
    const coords = (geometry as Point).getCoordinates(); // [lon, lat]
  
    const newPoint = {
      title: this.formName, // ✅ name değil title
      note: this.formDescription,
      longitude: coords[0],
      latitude: coords[1]
    };
  
    console.log("📦 Gönderilen nokta verisi:", newPoint);
  
    this.apiService.createPoint(newPoint).subscribe({
      next: (res) => this.zone.run(() => {
        this.sonCizilenFeature?.set('id', res.id);
        this.sonCizilenFeature?.set('name', this.formName); // Harita üzeri gösterim için
        this.sonCizilenFeature?.set('description', this.formDescription);
        this.closeActivePanel();
        this.tumEtkilesimleriDurdur();
      }),
      error: (err) => {
        console.error("❌ Nokta kaydedilemedi:", err.error);
        alert("Nokta kaydedilemedi:\n" + JSON.stringify(err.error));
      }
    });
  }
  
  
  loadExistingAreas(): void {
    this.apiService.getAllAreas().subscribe({
      next: (data) => {
        if (data?.type === 'FeatureCollection' && Array.isArray(data.features)) {
          const geoJsonFormat = new GeoJSON();
          const features = geoJsonFormat.readFeatures(data, {
            dataProjection: 'EPSG:4326',
            featureProjection: this.map.getView().getProjection()
          });
  
          features.forEach(f => f.set('feature_type', 'area'));
  
          this.areaVectorSource.clear();
          this.areaVectorSource.addFeatures(features);
        } else {
          console.error('❌ Geçersiz alan verisi formatı:', data);
        }
      },
      error: (err) => console.error('Alanlar yüklenirken hata:', err)
    });
  }
  
  

  loadExistingPoints(): void {
    this.apiService.getAllPoints().subscribe({
      next: (data: any[]) => {
        if (!Array.isArray(data)) {
          console.error('Beklenmeyen veri formatı:', data);
          return;
        }
  
        const features = data.map(dto => {
          if (
            typeof dto.longitude !== 'number' ||
            typeof dto.latitude !== 'number' ||
            typeof dto.id === 'undefined'
          ) {
            console.warn('Geçersiz nokta verisi atlandı:', dto);
            return null;
          }
  
          // Burada gelen lon/lat doğrudan EPSG:4326 olduğu için fromLonLat kullan
          const feature = new Feature({
            geometry: new Point(fromLonLat([dto.longitude, dto.latitude]))
          });
  
          feature.set('id', dto.id);
          feature.set('name', dto.title || '');        // backend'de Title olarak geldiyse burası title olmalı
          feature.set('description', dto.description || '');  // backend description olarak döndürdüyse burası description olmalı
          feature.set('feature_type', 'point');
          return feature;
        }).filter(f => f !== null);
  
        this.pointVectorSource.clear();
        this.pointVectorSource.addFeatures(features as Feature[]);
      },
      error: (err) => console.error('❌ Noktalar yüklenirken hata:', err)
    });
  }
  
  
  handleKeyDown(event: KeyboardEvent): void {
    if (event.key === 'Escape') {
      this.zone.run(() => {
        this.closeActivePanel();                // Açık panel varsa kapat
        this.tumEtkilesimleriDurdur();          // Harita üzerindeki etkileşimleri temizle
        this.contextMenuVisible = false;        // Sağ tıklama menüsünü gizle
        this.contextMenuFeature = null;         // Seçilen feature'ı temizle
        this.selectedFeatureId = null;          // Seçilen ID'yi sıfırla
      });
    }
  }
  
  logout(): void {
    this.apiService.logout(); // Oturumu sonlandır
  }}
