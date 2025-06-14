﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using testpro.Models;

namespace testpro.Services
{
    public class DrawingService : INotifyPropertyChanged
    {
        public List<Wall> Walls { get; } = new List<Wall>();
        public List<Room> Rooms { get; } = new List<Room>();
        public List<StoreObject> StoreObjects { get; } = new List<StoreObject>();

        private const double SnapDistance = 10.0;

        // X축과 Y축 스케일을 별도로 관리
        private double _scaleX = 1.0;
        private double _scaleY = 1.0;

        public double ScaleX
        {
            get => _scaleX;
            private set
            {
                if (value <= 0) value = 1.0; // 0 이하 방지
                _scaleX = value;
                OnPropertyChanged();
            }
        }

        public double ScaleY
        {
            get => _scaleY;
            private set
            {
                if (value <= 0) value = 1.0; // 0 이하 방지
                _scaleY = value;
                OnPropertyChanged();
            }
        }

        // 기존 Scale 속성은 호환성을 위해 유지 (평균값 반환)
        public double Scale
        {
            get => (_scaleX + _scaleY) / 2.0;
            private set
            {
                ScaleX = value;
                ScaleY = value;
            }
        }

        public void SetScale(double scale)
        {
            Scale = scale;
        }

        public void SetScaleXY(double scaleX, double scaleY)
        {
            ScaleX = scaleX;
            ScaleY = scaleY;
        }

        // 배경 이미지 관련
        private string _backgroundImagePath;
        public string BackgroundImagePath
        {
            get => _backgroundImagePath;
            set
            {
                _backgroundImagePath = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public Point2D SnapToExistingPoint(Point2D point)
        {
            foreach (var wall in Walls)
            {
                if (point.DistanceTo(wall.StartPoint) < SnapDistance)
                    return wall.StartPoint;

                if (point.DistanceTo(wall.EndPoint) < SnapDistance)
                    return wall.EndPoint;
            }
            return point;
        }

        public Wall AddWall(Point2D startPoint, Point2D endPoint)
        {
            var snappedStart = SnapToExistingPoint(startPoint);
            var snappedEnd = SnapToExistingPoint(endPoint);

            var wall = new Wall(snappedStart, snappedEnd);
            Walls.Add(wall);

            UpdateRooms();
            NotifyChanged();
            return wall;
        }

        // 매장 객체 관련 메서드
        public StoreObject AddStoreObject(ObjectType type, Point2D position)
        {
            var obj = new StoreObject(type, position);
            StoreObjects.Add(obj);
            NotifyChanged();
            return obj;
        }

        public void RemoveStoreObject(StoreObject obj)
        {
            StoreObjects.Remove(obj);
            NotifyChanged();
        }

        public void UpdateStoreObject(StoreObject obj, double height, int layers, bool isHorizontal)
        {
            obj.Height = height;
            obj.Layers = layers;
            obj.IsHorizontal = isHorizontal;
            obj.Rotation = isHorizontal ? 0 : 90;

            NotifyChanged();
        }

        public StoreObject GetObjectAt(Point2D point)
        {
            // 역순으로 검색 (위에 있는 객체 우선)
            for (int i = StoreObjects.Count - 1; i >= 0; i--)
            {
                if (StoreObjects[i].ContainsPoint(point))
                {
                    return StoreObjects[i];
                }
            }
            return null;
        }

        public void Clear()
        {
            Walls.Clear();
            Rooms.Clear();
            StoreObjects.Clear();
            BackgroundImagePath = null;
            // 스케일 초기화
            ScaleX = 1.0;
            ScaleY = 1.0;
            NotifyChanged();
        }

        private void UpdateRooms()
        {
            Rooms.Clear();
            var processedWalls = new HashSet<Wall>();

            foreach (var wall in Walls)
            {
                if (processedWalls.Contains(wall)) continue;

                var room = TryCreateRoomFromWall(wall, processedWalls);
                if (room != null && room.IsClosedRoom() && room.Area > 100)
                {
                    Rooms.Add(room);
                }
            }
        }

        private Room TryCreateRoomFromWall(Wall startWall, HashSet<Wall> processedWalls)
        {
            var room = new Room();
            var currentWalls = new List<Wall>();
            var visited = new HashSet<Wall>();

            if (FindConnectedWalls(startWall, currentWalls, visited))
            {
                foreach (var wall in currentWalls)
                {
                    room.AddWall(wall);
                    processedWalls.Add(wall);
                }
                return room;
            }

            return null;
        }

        private bool FindConnectedWalls(Wall startWall, List<Wall> roomWalls, HashSet<Wall> visited)
        {
            if (visited.Contains(startWall)) return false;

            visited.Add(startWall);
            roomWalls.Add(startWall);

            var connectedWalls = GetConnectedWalls(startWall);

            foreach (var connected in connectedWalls)
            {
                if (!visited.Contains(connected))
                {
                    if (FindConnectedWalls(connected, roomWalls, visited))
                        return true;
                }
                else if (connected == roomWalls.First() && roomWalls.Count >= 3)
                {
                    return true;
                }
            }

            return false;
        }

        private List<Wall> GetConnectedWalls(Wall wall)
        {
            var connected = new List<Wall>();

            foreach (var other in Walls)
            {
                if (other != wall && wall.IsConnectedTo(other))
                {
                    connected.Add(other);
                }
            }

            return connected;
        }

        private void NotifyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}