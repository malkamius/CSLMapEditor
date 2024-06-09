using CrimsonStainedLands;
using CrimsonStainedLands.Extensions;
using SkiaSharp;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;

namespace Mapper2
{

    public partial class MainWindow : Form
    {
        bool pauseUpdate = false;

        AreaData? drawnArea = null;
        private Dictionary<RoomData, (int Zone, Drawer.Box Box)> RoomsDraw = new Dictionary<RoomData, (int Zone, Drawer.Box Box)>();
        //private Dictionary<Drawer.Box, RoomData> DrawnBoxes = new Dictionary<Drawer.Box, RoomData>();
        public MainWindow()
        {
            InitializeComponent();
            CrimsonStainedLands.Settings.DataPath = "..\\..\\..\\data";
            CrimsonStainedLands.Settings.AreasPath = "..\\..\\..\\data\\areas";
            CrimsonStainedLands.Settings.RacesPath = "..\\..\\..\\data\\races";
            CrimsonStainedLands.Settings.GuildsPath = "..\\..\\..\\data\\guilds";
            CrimsonStainedLands.Settings.PlayersPath = "..\\..\\..\\data\\players";

        }

        private void MainWindow_Load(object sender, EventArgs e)
        {
            Settings.Load();
            WeaponDamageMessage.LoadWeaponDamageMessages();
            Race.LoadRaces();
            SkillSpellGroup.LoadSkillSpellGroups();

            GuildData.LoadGuilds();

            WeaponDamageMessage.LoadWeaponDamageMessages();

            CrimsonStainedLands.AreaData.LoadAreas(false);

            foreach (var area in AreaData.Areas)
                //area = new AreaData(area.fileName, false);
                foreach (var exitfixroom in area.Rooms.Values)
                    foreach (var exit in exitfixroom.exits)
                    {
                        if (exit != null)
                        {
                            RoomData.Rooms.TryGetValue(exit.destinationVnum, out exit.destination);
                            exit.source = exitfixroom;
                        }

                    }

            //font = new Font(this.Font.FontFamily, 7);
            sectorComboBox.Items.AddRange((from sector in Utility.GetEnumValues<SectorTypes>() select ((object)sector.ToString())).ToArray());

            selectorTreeView.Nodes.AddRange((from area in CrimsonStainedLands.AreaData.Areas orderby area.Name select new TreeNode(area.Name) { Tag = area }).ToArray());

            foreach (var node in selectorTreeView.Nodes.OfType<TreeNode>())
            {
                var roomsnode = node.Nodes.Add("Rooms");
                var itemsnode = node.Nodes.Add("Items");
                var npcsnode = node.Nodes.Add("NPCs");
                var resetsnode = node.Nodes.Add("Resets");
                roomsnode.Nodes.AddRange((from room in ((AreaData)node.Tag).Rooms select new TreeNode(room.Key + " - " + room.Value.Name) { Tag = room.Value }).ToArray());

            }

        }
        bool wholemapdrawn = false;

        private void selectorTreeView_AfterSelect(object sender, TreeViewEventArgs e)
        {
            if (e.Node.Tag is AreaData)
            {
                //if (!wholemapdrawn || !drawWholeWorldCheckBox.Checked)
                drawMap((AreaData)e.Node.Tag);
            }
            else if (e.Node.Tag is RoomData)
                selectNode((from room in RoomsDraw.Keys where room == e.Node.Tag select room).FirstOrDefault());

            if (e.Node.Text == "Items" && e.Node.Parent != null && e.Node.Parent.Tag is AreaData)
            {
                var itemsWindow = new ItemsWindow((AreaData)e.Node.Parent.Tag);
                itemsWindow.Show(this);
            }
            else if (e.Node.Text == "NPCs" && e.Node.Parent != null && e.Node.Parent.Tag is AreaData)
            {
                var NPCsWindow = new NPCsWindow((AreaData)e.Node.Parent.Tag);
                NPCsWindow.Show(this);

            }
            else if (e.Node.Text == "Resets" && e.Node.Parent != null && e.Node.Parent.Tag is AreaData)
            {
                var ResetsWindow = new ResetsWindow((AreaData)e.Node.Parent.Tag);
                ResetsWindow.Show(this);

            }
        }

        Bitmap? drawBoxes(AreaData area)
        {
            var bitmaps = new List<SKBitmap>();
            var ZoneXOffset = 0;
            foreach (var zone in RoomsDraw.Select(z => z.Value.Zone).Distinct())
            {
                Drawer.Boxes.Clear();

                foreach (var mappedroom in RoomsDraw.Where(b => b.Value.Zone == zone))
                {
                    var box = mappedroom.Value.Box;
                    //if (mappedroom.Value.Zone == zone)
                    if (box == null)
                    {
                        box = new Drawer.Box();
                        box.x = mappedroom.Value.Box.x;
                        box.y = mappedroom.Value.Box.y;

                        box.height = 50;
                        box.width = 50;
                    }

                    Drawer.Boxes.Add(box);
                    if (string.IsNullOrEmpty(box.text))
                    {
                        if (mappedroom.Key.Area == area)
                        {
                            box.BackColor = SkiaSharp.SKColors.LightYellow;
                            box.OriginalBackColor = box.BackColor;
                            box.text = mappedroom.Key.Name;
                        }
                        else
                        {
                            box.text = "To " + mappedroom.Key.Area.Name;
                            box.BackColor = SkiaSharp.SKColors.White;
                            box.OriginalBackColor = box.BackColor;
                        }
                    }

                    //rooms.Add(mappedroom.Key, box);
                }

                foreach (var rd in RoomsDraw.Where(b => b.Value.Zone == zone))
                {
                    if (rd.Value.Box.Exits.Count == 0)
                        foreach (var exit in rd.Key.exits)
                        {
                            KeyValuePair<RoomData, (int Zone, Drawer.Box Box)>? DestinationBox = null;

                            //if (exit != null && exit.destination != null &&  rooms.TryGetValue(exit.destination, out var destinationbox))
                            if (exit != null && exit.destination != null && (DestinationBox = RoomsDraw.FirstOrDefault(b => b.Key == exit.destination && b.Value.Zone == rd.Value.Zone)).HasValue && DestinationBox.Value.Value.Box != null)
                                rd.Value.Box.Exits.Add(exit.direction, DestinationBox.Value.Value.Box);
                        }
                }

                var skbmp = Drawer.Draw(ZoneXOffset);
                if (skbmp != null)
                {
                    bitmaps.Add(skbmp);
                    ZoneXOffset += skbmp.Width;
                }
            }


            //foreach (var mappedroom in rooms)
            //{
            //    foreach (var exit in mappedroom.Key.exits)
            //    {

            //        if (exit != null && exit.destination != null && rooms.TryGetValue(exit.destination, out var destinationbox))
            //            mappedroom.Value.Exits.Add(exit.direction, destinationbox);
            //    }
            //}


            if (bitmaps.Any())
            {
                var bitmap = new Bitmap(bitmaps.Sum(b => b.Width), bitmaps.Max(b => b.Height));
                var x = 0;
                using (var g = Graphics.FromImage(bitmap))
                {
                    g.Clear(Color.White);
                    foreach (var skbmp in bitmaps)
                    {
                        using (var tmp = new Bitmap(skbmp.Width, skbmp.Height))
                        {
                            var data = tmp.LockBits(new Rectangle(0, 0, tmp.Width, tmp.Height),
                                        System.Drawing.Imaging.ImageLockMode.WriteOnly, System.Drawing.Imaging.PixelFormat.Format32bppPArgb);
                            IntPtr ptr = data.Scan0;
                            int size = skbmp.RowBytes * skbmp.Height;
                            System.Runtime.InteropServices.Marshal.Copy(skbmp.Bytes, 0, ptr, size);
                            skbmp.Dispose();
                            tmp.UnlockBits(data);
                            g.DrawImage(tmp, x, 0);
                            x += tmp.Width;
                        }

                    }
                }

                return bitmap;
            }
            return null;
        }

        void drawMap(AreaData area)
        {
            wholemapdrawn = drawWholeWorldCheckBox.Checked;


            mapPanel.Enabled = false;

            if (pictureBox1.Image != null)
            {
                pictureBox1.Image.Dispose();
                pictureBox1.Image = null;
            }

            var mapper = new Mapper.AreaMapper();
            mapper.MapRooms(area);
            Text = mapper.roomPositions.Count + " rooms/area links mapped";
            drawnArea = area;
            RoomsDraw.Clear();
            foreach (var position in mapper.roomPositions)
            {
                RoomsDraw.Add(position.Key, (position.Value.Zone, new Drawer.Box() { x = position.Value.X, y = position.Value.Y }));
            }

            pictureBox1.Image = drawBoxes(area);
            pictureBox1.Parent = mapPanel;
            mapPanel.ResumeLayout();
            Application.DoEvents();

            mapPanel.Enabled = true;
        }

        private void selectNode(RoomData? room)
        {
            var room2 = room;
            if (room2 == null)
            {
                return;
            }

            var areanode = (from n in selectorTreeView.Nodes.OfType<TreeNode>() where n.Tag == room.Area select n).FirstOrDefault();

            if (areanode != null)
            {
                var roomsnode = (from node in areanode.Nodes.OfType<TreeNode>() where node.Text == "Rooms" select node).FirstOrDefault();

                if (roomsnode != null)
                {
                    var roomnode = (from r in roomsnode.Nodes.OfType<TreeNode>() where r.Tag == room select r).FirstOrDefault();
                    if (roomnode != null)
                    {
                        selectorTreeView.SelectedNode = roomnode;


                        //Drawer.Box box = Drawer.Boxes.Where((Drawer.Box b) => b.wrapper == room2).FirstOrDefault();
                        //if (box != null)
                        //{
                        //    panel1.HorizontalScroll.Value = Math.Min(panel1.HorizontalScroll.Maximum, Math.Max(0, box.x + Drawer.Origin.X - panel1.Width / 2));
                        //    panel1.VerticalScroll.Value = Math.Min(panel1.VerticalScroll.Maximum, Math.Max(0, box.y + Drawer.Origin.Y - panel1.Height / 2));
                        //}
                        //foreach (KeyValuePair<int, AreaMapper> room3 in rooms)
                        //{
                        //    room3.Value.Box.BackColor = Brushes.White;
                        //}
                        foreach (var artifact in RoomsDraw)
                            artifact.Value.Box.BackColor = artifact.Value.Box.OriginalBackColor;
                        var selectedroomdraw = RoomsDraw.FirstOrDefault(kvp => kvp.Key == room);

                        if (selectedroomdraw.Key != null)
                        {
                            selectedroomdraw.Value.Box.BackColor = SKColors.LightBlue;

                            selectorTreeView.SelectedNode = roomnode;
                            //room2.Box.BackColor = Brushes.Aqua;
                            if (pictureBox1.Image != null)
                            {
                                pictureBox1.Image.Dispose();
                            }
                            pictureBox1.Image = drawBoxes(room.Area);
                            panel1.HorizontalScroll.Value = Math.Min(panel1.HorizontalScroll.Maximum, Math.Max(0, selectedroomdraw.Value.Box.drawlocation.X + selectedroomdraw.Value.Box.XOffsetForZone));
                            panel1.VerticalScroll.Value = Math.Min(panel1.VerticalScroll.Maximum, Math.Max(0, selectedroomdraw.Value.Box.drawlocation.Y));
                        }
                        //if (box != null)
                        //{

                        //}
                        //foreach (KeyValuePair<int, AreaMapper> room3 in rooms)
                        //{
                        //    room3.Value.Box.BackColor = Brushes.White;
                        //}

                        //pictureBox1.Image = Drawer.Draw();
                        pauseUpdate = true;
                        EditingRoom = room;
                        VnumText.Text = room.Vnum.ToString();
                        roomNameTextBox.Text = room.Name;
                        roomDescTextBox.Text = room.Description.Replace("\n", Environment.NewLine);
                        exitDirectionComboBox.SelectedIndex = 0;

                        sectorComboBox.SelectedIndex = sectorComboBox.Items.IndexOf(room.sector.ToString());
                        updateExit();
                        pauseUpdate = false;
                    }
                }
            }
        }

        public RoomData EditingRoom = null;

        private void filterTextBox_TextChanged(object sender, EventArgs e)
        {
            var node = (from n in selectorTreeView.Nodes.OfType<TreeNode>() where n.Text.ToLower().StartsWith(filterTextBox.Text.ToLower()) select n).FirstOrDefault();

            if (node != null)
            {
                selectorTreeView.SelectedNode = node;
            }

        }

        private void panel1_Paint(object sender, PaintEventArgs e)
        {

        }

        private void exitDirectionComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            updateExit();
        }

        private void updateExit()
        {
            if (EditingRoom != null)
            {
                Direction direction = Direction.North;
                Utility.GetEnumValue<Direction>(exitDirectionComboBox.Text, ref direction);
                if (EditingRoom.exits[(int)direction] == null)
                {
                    //EditingRoom.room.exits[(int)direction] = new ExitData() { direction = direction };
                    exitDescriptionTextBox.Text = "";
                    //if (exit.destination != null)
                    //    exit.destinationVnum = exit.destination.vnum;
                    exitDestinationTextBox.Text = "0";

                    windowCheckBox.Checked = false;
                    doorCheckBox.Checked = false;
                    closedCheckBox.Checked = false;
                    lockedCheckBox.Checked = false;
                }
                else
                {
                    var exit = EditingRoom.exits[(int)direction];
                    exitDescriptionTextBox.Text = exit.description;
                    //if (exit.destination != null)
                    //    exit.destinationVnum = exit.destination.vnum;
                    exitDestinationTextBox.Text = exit.destinationVnum.ToString();

                    windowCheckBox.Checked = exit.flags.ISSET(ExitFlags.Window);
                    doorCheckBox.Checked = exit.flags.ISSET(ExitFlags.Door);
                    closedCheckBox.Checked = exit.flags.ISSET(ExitFlags.Closed);
                    lockedCheckBox.Checked = exit.flags.ISSET(ExitFlags.Locked);
                }
            }
        }

        private void SaveExit()
        {
            if (EditingRoom != null && !pauseUpdate)
            {
                Direction direction = Direction.North;
                Utility.GetEnumValue<Direction>(exitDirectionComboBox.Text, ref direction);
                if (EditingRoom.exits[(int)direction] == null)
                {
                    EditingRoom.exits[(int)direction] = new ExitData() { direction = direction };

                }
                var exit = EditingRoom.exits[(int)direction];
                exit.description = exitDescriptionTextBox.Text;
                int.TryParse(exitDestinationTextBox.Text, out exit.destinationVnum);

                if (windowCheckBox.Checked)
                    exit.flags.SETBIT(ExitFlags.Window);
                else
                    exit.flags.REMOVEFLAG(ExitFlags.Window);

                if (doorCheckBox.Checked)
                    exit.flags.SETBIT(ExitFlags.Door);
                else
                    exit.flags.REMOVEFLAG(ExitFlags.Door);

                if (closedCheckBox.Checked)
                    exit.flags.SETBIT(ExitFlags.Closed);
                else
                    exit.flags.REMOVEFLAG(ExitFlags.Closed);

                if (lockedCheckBox.Checked)
                    exit.flags.SETBIT(ExitFlags.Locked);
                else
                    exit.flags.REMOVEFLAG(ExitFlags.Locked);
                EditingRoom.Area.saved = false;
            }
        }

        private void exitDescriptionTextBox_TextChanged(object sender, EventArgs e)
        {
            SaveExit();
        }

        private void exitDestinationTextBox_TextChanged(object sender, EventArgs e)
        {
            SaveExit();
        }

        private void doorCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            SaveExit();
        }

        private void windowCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            SaveExit();
        }

        private void closedCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            SaveExit();
        }

        private void lockedCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            SaveExit();
        }

        private void roomDescTextBox_TextChanged(object sender, EventArgs e)
        {
            if (!pauseUpdate && EditingRoom != null)
            {
                EditingRoom.Area.saved = false;
                EditingRoom.Description = roomDescTextBox.Text;
            }
        }

        private void roomNameTextBox_TextChanged(object sender, EventArgs e)
        {
            if (!pauseUpdate && EditingRoom != null)
            {
                EditingRoom.Name = roomNameTextBox.Text;
                EditingRoom.Area.saved = false;

                var areanode = (from n in selectorTreeView.Nodes.OfType<TreeNode>() where n.Tag == EditingRoom.Area select n).FirstOrDefault();

                if (areanode != null)
                {
                    var roomsnode = (from node in areanode.Nodes.OfType<TreeNode>() where node.Text == "Rooms" select node).FirstOrDefault();

                    if (roomsnode != null)
                    {
                        var roomnode = (from r in roomsnode.Nodes.OfType<TreeNode>() where r.Tag == EditingRoom select r).FirstOrDefault();
                        if (roomnode != null)
                        {
                            roomnode.Text = EditingRoom.Vnum + " - " + EditingRoom.Name;
                        }
                    }
                }
            }
        }

        private void sectorComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!pauseUpdate && EditingRoom != null)
            {
                Utility.GetEnumValue<SectorTypes>(sectorComboBox.SelectedItem.ToString(), ref EditingRoom.sector);
                EditingRoom.Area.saved = false;
            }
        }

        private void saveWorldButton_Click(object sender, EventArgs e)
        {
            var unsaved = AreaData.Areas.Count(a => a.saved == false);
            AreaData.DoASaveWorlds(null, null);
            var saved = AreaData.Areas.Count(a => a.saved == false);
            MessageBox.Show(unsaved - saved + " areas saved. " + saved + " unsaved.");
        }

        private void Dig(Direction direction)
        {
            var vnum = EditingRoom.Area.Rooms.Count > 0 ? EditingRoom.Area.Rooms.Max(r => r.Key) + 1 : EditingRoom.Area.VNumStart;
            Dictionary<Direction, Direction> reverseDirections = new Dictionary<Direction, Direction>
            { { Direction.North, Direction.South }, { Direction.East, Direction.West },
                {Direction.South, Direction.North } , {Direction.West, Direction.East },
                {Direction.Up, Direction.Down }, {Direction.Down, Direction.Up } };
            RoomData room;
            if (RoomData.Rooms.ContainsKey(vnum))
            {
                //ch.send("Not yet implemented.\n\r");
                MessageBox.Show("That vnum is already taken.");
                return;
            }
            else
            {
                room = new RoomData();
                room.Vnum = vnum;
                room.Area = EditingRoom.Area;
                room.Area.Rooms.Add(vnum, room);
                pauseUpdate = true;
                if (copyNameAndDescCheckBox.Checked)
                {
                    room.Name = EditingRoom.Name;
                    room.Description = EditingRoom.Description;
                }
                else
                {
                    room.Name = "New Room";
                    room.Description = "";
                }
                room.sector = EditingRoom.sector;
                RoomData.Rooms.Add(vnum, room);
            }
            room.Area.saved = false;
            EditingRoom.Area.saved = false;
            var revDirection = reverseDirections[direction];
            var flags = new List<ExitFlags>();

            room.exits[(int)revDirection] = new ExitData() { destination = EditingRoom, destinationVnum = EditingRoom.Vnum, direction = revDirection, description = "", flags = new List<ExitFlags>(), originalFlags = new List<ExitFlags>() };
            EditingRoom.exits[(int)direction] = new ExitData() { destination = room, direction = direction, description = "", flags = new List<ExitFlags>(), originalFlags = new List<ExitFlags>() };

            selectorTreeView.Nodes.OfType<TreeNode>().First(n => n.Tag == room.Area).Nodes.OfType<TreeNode>().First(n => n.Text == "Rooms").Nodes.Add(new TreeNode(room.Vnum + " - " + room.Name) { Tag = room });


            drawMap(EditingRoom.Area);

            selectNode(room);
        }

        private void digNorthButton_Click(object sender, EventArgs e)
        {
            Dig(Direction.North);
        }

        private void digEastButton_Click(object sender, EventArgs e)
        {
            Dig(Direction.East);
        }

        private void digSouthButton_Click(object sender, EventArgs e)
        {
            Dig(Direction.South);
        }

        private void digWestButton_Click(object sender, EventArgs e)
        {
            Dig(Direction.West);
        }

        private void digUpButton_Click(object sender, EventArgs e)
        {
            Dig(Direction.Up);
        }

        private void digDownButton_Click(object sender, EventArgs e)
        {
            Dig(Direction.Down);
        }

        private void saveMapImageButton_Click(object sender, EventArgs e)
        {
            var imagename = "Map.jpg";
            if (drawnArea != null && !string.IsNullOrEmpty(drawnArea.Name)) imagename = string.Format("Map of {0}.jpg", drawnArea.Name);
            foreach (var ch in System.IO.Path.GetInvalidFileNameChars())
                imagename = imagename.Replace(ch, ' ');

            if (pictureBox1.Image != null) { }
            pictureBox1.Image.Save(imagename, ImageFormat.Jpeg);
        }

        private void selectorTreeView_Click(object sender, EventArgs e)
        {

        }

        private void VnumText_TextChanged(object sender, EventArgs e)
        {
            if (!pauseUpdate && EditingRoom != null)
            {

                int vnum;
                if (int.TryParse(VnumText.Text, out vnum) && !RoomData.Rooms.ContainsKey(vnum))
                {
                    RoomData.Rooms[EditingRoom.Vnum] = null;
                    EditingRoom.Area.Rooms[EditingRoom.Vnum] = null;
                    EditingRoom.Vnum = vnum;
                    EditingRoom.Area.saved = false;
                    RoomData.Rooms[EditingRoom.Vnum] = EditingRoom;
                    EditingRoom.Area.Rooms[EditingRoom.Vnum] = EditingRoom;

                    var areanode = (from n in selectorTreeView.Nodes.OfType<TreeNode>() where n.Tag == EditingRoom.Area select n).FirstOrDefault();
                    if (areanode != null)
                    {
                        var roomsnode = (from node in areanode.Nodes.OfType<TreeNode>() where node.Text == "Rooms" select node).FirstOrDefault();

                        if (roomsnode != null)
                        {
                            var roomnode = (from r in roomsnode.Nodes.OfType<TreeNode>() where r.Tag == EditingRoom select r).FirstOrDefault();
                            if (roomnode != null)
                            {

                                roomnode.Text = EditingRoom.Vnum + " - " + EditingRoom.Name;

                            }
                        }
                    }
                }
            }
        }

        private void ToolTip_Popup(object sender, PopupEventArgs e)
        {

        }

        private void pictureBox1_MouseClick(object sender, MouseEventArgs e)
        {
            MouseEventArgs e2 = e;
            var rd = RoomsDraw.FirstOrDefault(q => e2.X >= q.Value.Box.drawlocation.X + q.Value.Box.XOffsetForZone && e2.X <= q.Value.Box.drawlocation.Right + q.Value.Box.XOffsetForZone && e2.Y >= q.Value.Box.drawlocation.Y && e2.Y <= q.Value.Box.drawlocation.Bottom);
            if (rd.Key != null)
            {
                if (rd.Key.Area != drawnArea)
                    selectorTreeView.SelectedNode = selectorTreeView.Nodes.OfType<TreeNode>().FirstOrDefault(n => n.Tag == rd.Key.Area);
                selectNode(rd.Key);
            }
        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {

        }
    }
}