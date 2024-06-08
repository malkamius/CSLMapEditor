using CrimsonStainedLands;
using CrimsonStainedLands.Extensions;
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
        private Font font;
        Dictionary<int, RoomWrapper> rooms = new Dictionary<int, RoomWrapper>();

        public List<MapRoomOp> roomOpList = new List<MapRoomOp>();
        public MainWindow()
        {
            InitializeComponent();
            CrimsonStainedLands.Settings.DataPath = "..\\..\\..\\data";
            CrimsonStainedLands.Settings.AreasPath = "..\\..\\..\\data\\areas";
            CrimsonStainedLands.Settings.RacesPath = "..\\..\\..\\data\\races";
            CrimsonStainedLands.Settings.GuildsPath= "..\\..\\..\\data\\guilds";
            CrimsonStainedLands.Settings.PlayersPath= "..\\..\\..\\data\\players";

        }

        private void MainWindow_Load(object sender, EventArgs e)
        {
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

            font = new Font(this.Font.FontFamily, 7);
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
                if (!wholemapdrawn || !drawWholeWorldCheckBox.Checked)
                    drawMap((AreaData)e.Node.Tag);
            }
            else if (e.Node.Tag is RoomData)
                selectNode((from room in rooms where room.Value.room == e.Node.Tag select room.Value).FirstOrDefault());

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


        private int xoffset = 0;
        private int yoffset = 0;

        void drawMap(AreaData area)
        {
            wholemapdrawn = drawWholeWorldCheckBox.Checked;
            rooms.Clear();
            mapPanel.Controls.Clear();
            Drawer.Boxes.Clear();
            xoffset = 0;
            yoffset = 0;
            //((ScrollableControl)mapPanel).AutoScrollOffset = new Point(0, 0);
            mapPanel.Enabled = false;
            if (area is AreaData)
            {
                //var area = (AreaData)e.Node.Tag;


                //var room = area.rooms.Values.FirstOrDefault();
                var room = area.Rooms.Values.FirstOrDefault();
                mapRoom(room, 5, 5, 0);
                int i = 0;
                while (roomOpList.Count > 0)
                {
                    var op = roomOpList.OrderByDescending(i => i.room.Area.VNumStart).First();
                    roomOpList.Remove(op);

                    if (drawWholeWorldCheckBox.Checked || op.room.Area == area)
                        //if(op.room.Area == e.Node.Tag)
                        mapRoom(op.room, op.x, op.y, op.z);
                    i++;
                    if (i % 100 == 0)
                    {
                        Text = rooms.Count + " rooms mapped";
                        mapPanel.ResumeLayout();
                        Application.DoEvents();
                        mapPanel.SuspendLayout();
                    }
                }

                foreach (var cslroom in area.Rooms)
                {
                    if (!rooms.ContainsKey(cslroom.Key))
                    {
                        //var roomWrapper = rooms[room.Vnum] = new RoomWrapper() { room = room, vnum = room.Vnum, x = 5, y = 5, z = 1 };
                        mapRoom(cslroom.Value, 5, 5, 1);
                    }
                }
            }


            int unmappedRoomsCount = 0;
            foreach (var vnum in RoomData.Rooms.Keys)
            {
                if (!rooms.ContainsKey(vnum))
                    unmappedRoomsCount++;
            }

            Text = rooms.Count + " rooms mapped - " + unmappedRoomsCount + " not connected or mapped";

            if (pictureBox1.Image != null)
            {
                pictureBox1.Image.Dispose();
            }
            pictureBox1.Image = Drawer.Draw();

            ////var roomsordered = rooms.OrderBy(r => r.Key).ToList(); // displays wrong
            ////foreach (var room in roomsordered) // rooms.OrderBy(r => r.Key))
            ////{
            ////    if (room.Value.label != null)
            ////        //room.Value.label.BringToFront();
            ////        room.Value.label.Parent.Controls.SetChildIndex(room.Value.label, roomsordered.IndexOf(room));
            ////}
            pictureBox1.Parent = mapPanel;
            mapPanel.ResumeLayout();
            Application.DoEvents();

            mapPanel.Enabled = true;

            if (drawWholeWorldCheckBox.Checked)
            {

            }
        }

        const int tilesize = 60;
        void mapRoom(RoomData room, int x, int y, int z)
        {
            RoomWrapper roomWrapper;
            if (room == null) return;

            //@Html.Raw("<div style='display:block;width:50px;height:50px; margin: auto;position:absolute;border: 1px solid black;top:" + (y * 60) + "px;left:" + (x * 60) + "px'>" + room.Vnum + "</div>")
            //if (z == 0)

            if (!rooms.TryGetValue(room.Vnum, out roomWrapper) || roomWrapper == null)
            {
                roomWrapper = rooms[room.Vnum] = new RoomWrapper() { room = room, vnum = room.Vnum, x = x, y = y, z = z };
                //if (z == 0)
                {


                    ////var label = new Label();
                    ////label.BorderStyle = BorderStyle.Fixed3D;

                    ////mapPanel.Controls.Add(label);
                    ////bool offsetchanged = false;
                    ////if (xoffset + x < 0)
                    ////{
                    ////    offsetchanged = true;
                    ////    xoffset += 5;
                    ////}
                    ////if (yoffset + y < 0)
                    ////{
                    ////    offsetchanged = true;
                    ////    yoffset += 5;
                    ////}

                    ////label.Left = ((x + xoffset) * tilesize);
                    ////label.Top = ((y + yoffset) * tilesize);
                    ////label.Width = tilesize;
                    ////label.Height = tilesize;
                    ////if (wholemapdrawn)
                    ////{
                    ////    label.Text = room.Area.name + Environment.NewLine + room.Vnum;
                    ////}
                    ////else
                    ////    label.Text = room.Vnum + Environment.NewLine + room.Name;
                    ////ToolTip.SetToolTip(label, room.Vnum + Environment.NewLine + room.Name);
                    ////label.Font = font;
                    ////label.Click += Label_Click;
                    ////label.Tag = roomWrapper;
                    ////label.BackColor = Color.White;
                    ////if (z != 0) label.SendToBack();
                    ////roomWrapper.label = label;

                    Drawer.Box box = (roomWrapper.Box = new Drawer.Box
                    {
                        x = x * 50,
                        y = y * 50,
                        width = 50,
                        height = 50,
                        text = room.Area.Name + Environment.NewLine + room.Vnum,
                        wrapper = roomWrapper
                    });
                    Drawer.Boxes.Add(box);
                    ////if (offsetchanged)
                    ////    foreach (var roomwrapper in rooms.Values)
                    ////    {
                    ////        if (roomwrapper != roomWrapper)
                    ////        {
                    ////            if (roomwrapper.label != null)
                    ////            {
                    ////                roomwrapper.label.Left = ((roomwrapper.x + xoffset) * tilesize);
                    ////                roomwrapper.label.Top = ((roomwrapper.y + yoffset) * tilesize);
                    ////            }
                    ////        }
                    ////    }
                }

                foreach (var exit in room.exits)
                {
                    if (exit != null && exit.destination != null && !rooms.ContainsKey(exit.destinationVnum))
                    {
                        int newy = y;
                        int newx = x;
                        int newz = z;
                        switch (exit.direction)
                        {
                            case Direction.North:
                                newy = y - 1;
                                break;
                            case Direction.East:
                                newx = x + 1;
                                break;
                            case Direction.South:
                                newy = y + 1;
                                break;
                            case Direction.West:
                                newx = x - 1;
                                break;
                            case Direction.Up:
                                newz = z + 1;
                                break;
                            case Direction.Down:
                                newz = z - 1;
                                break;
                        }
                        //mapRoom(exit.destination, newx, newy, newz);
                        roomOpList.Add(new MapRoomOp { room = exit.destination, x = newx, y = newy, z = newz });

                    }
                }
            }
        }

        private void Label_Click(object? sender, EventArgs e)
        {
            if (sender is Label && ((Label)sender).Tag is RoomWrapper)
            {
                selectNode(((RoomWrapper)((Label)sender).Tag));

            }

        }

        private void selectNode(RoomWrapper room)
        {
            RoomWrapper room2 = room;
            if (room2 == null)
            {
                return;
            }
            
            var areanode = (from n in selectorTreeView.Nodes.OfType<TreeNode>() where n.Tag == room.room.Area select n).FirstOrDefault();

            if (areanode != null)
            {
                var roomsnode = (from node in areanode.Nodes.OfType<TreeNode>() where node.Text == "Rooms" select node).FirstOrDefault();

                if (roomsnode != null)
                {
                    var roomnode = (from r in roomsnode.Nodes.OfType<TreeNode>() where r.Tag == room.room select r).FirstOrDefault();
                    if (roomnode != null)
                    {
                        //foreach (var otherroom in rooms)
                        //    if (otherroom.Value.label != null)
                        //        otherroom.Value.label.BackColor = Color.White;
                        selectorTreeView.SelectedNode = roomnode;
                        //if (room.label != null)
                        //{
                        //    room.label.BackColor = Color.Aqua;
                        //    room.label.BringToFront();
                        //}
                        //panel1.ScrollControlIntoView(room.label);

                        Drawer.Box box = Drawer.Boxes.Where((Drawer.Box b) => b.wrapper == room2).FirstOrDefault();
                        if (box != null)
                        {
                            panel1.HorizontalScroll.Value = Math.Min(panel1.HorizontalScroll.Maximum, Math.Max(0, box.x + Drawer.Origin.X - panel1.Width / 2));
                            panel1.VerticalScroll.Value = Math.Min(panel1.VerticalScroll.Maximum, Math.Max(0, box.y + Drawer.Origin.Y - panel1.Height / 2));
                        }
                        foreach (KeyValuePair<int, RoomWrapper> room3 in rooms)
                        {
                            room3.Value.Box.BackColor = Brushes.White;
                        }
                        selectorTreeView.SelectedNode = roomnode;
                        room2.Box.BackColor = Brushes.Aqua;
                        if (pictureBox1.Image != null)
                        {
                            pictureBox1.Image.Dispose();
                        }
                        pictureBox1.Image = Drawer.Draw();
                        pauseUpdate = true;
                        EditingRoom = room;
                        VnumText.Text = room.room.Vnum.ToString();
                        roomNameTextBox.Text = room.room.Name;
                        roomDescTextBox.Text = room.room.Description.Replace("\n", Environment.NewLine);
                        exitDirectionComboBox.SelectedIndex = 0;

                        sectorComboBox.SelectedIndex = sectorComboBox.Items.IndexOf(room.room.sector.ToString());
                        updateExit();
                        pauseUpdate = false;
                    }
                }
            }
        }

        public RoomWrapper EditingRoom = null;

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
                if (EditingRoom.room.exits[(int)direction] == null)
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
                    var exit = EditingRoom.room.exits[(int)direction];
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
                if (EditingRoom.room.exits[(int)direction] == null)
                {
                    EditingRoom.room.exits[(int)direction] = new ExitData() { direction = direction };

                }
                var exit = EditingRoom.room.exits[(int)direction];
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
                EditingRoom.room.Area.saved = false;
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
                EditingRoom.room.Area.saved = false;
                EditingRoom.room.Description = roomDescTextBox.Text;
            }
        }

        private void roomNameTextBox_TextChanged(object sender, EventArgs e)
        {
            if (!pauseUpdate && EditingRoom != null)
            {
                EditingRoom.room.Name = roomNameTextBox.Text;
                EditingRoom.room.Area.saved = false;

                var areanode = (from n in selectorTreeView.Nodes.OfType<TreeNode>() where n.Tag == EditingRoom.room.Area select n).FirstOrDefault();

                if (areanode != null)
                {
                    var roomsnode = (from node in areanode.Nodes.OfType<TreeNode>() where node.Text == "Rooms" select node).FirstOrDefault();

                    if (roomsnode != null)
                    {
                        var roomnode = (from r in roomsnode.Nodes.OfType<TreeNode>() where r.Tag == EditingRoom.room select r).FirstOrDefault();
                        if (roomnode != null)
                        {
                            roomnode.Text = EditingRoom.vnum + " - " + EditingRoom.room.Name;
                        }
                    }
                }
            }
        }

        private void sectorComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!pauseUpdate && EditingRoom != null)
            {
                Utility.GetEnumValue<SectorTypes>(sectorComboBox.SelectedItem.ToString(), ref EditingRoom.room.sector);
                EditingRoom.room.Area.saved = false;
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
            var vnum = EditingRoom.room.Area.Rooms.Count > 0 ? EditingRoom.room.Area.Rooms.Max(r => r.Key) + 1 : EditingRoom.room.Area.VNumStart;
            Dictionary<Direction, Direction> reverseDirections = new Dictionary<Direction, Direction>
            { { Direction.North, Direction.South }, { Direction.East, Direction.West },
                {Direction.South, Direction.North } , {Direction.West, Direction.East },
                {Direction.Up, Direction.Down }, {Direction.Down, Direction.Up } };
            RoomData room;
            if (RoomData.Rooms.TryGetValue(vnum, out room))
            {
                //ch.send("Not yet implemented.\n\r");
            }
            else
            {
                room = new RoomData();
                room.Vnum = vnum;
                room.Area = EditingRoom.room.Area;
                room.Area.Rooms.Add(vnum, room);
                pauseUpdate = true;
                if (copyNameAndDescCheckBox.Checked)
                {
                    room.Name = EditingRoom.room.Name;
                    room.Description = EditingRoom.room.Description;
                }
                room.sector = EditingRoom.room.sector;
                RoomData.Rooms.Add(vnum, room);
            }
            room.Area.saved = false;
            EditingRoom.room.Area.saved = false;
            var revDirection = reverseDirections[direction];
            var flags = new List<ExitFlags>();

            room.exits[(int)revDirection] = new ExitData() { destination = EditingRoom.room, destinationVnum = EditingRoom.room.Vnum, direction = revDirection, description = "", flags = new List<ExitFlags>(), originalFlags = new List<ExitFlags>() };
            EditingRoom.room.exits[(int)direction] = new ExitData() { destination = room, direction = direction, description = "", flags = new List<ExitFlags>(), originalFlags = new List<ExitFlags>() };

            int newy = EditingRoom.y;
            int newx = EditingRoom.x;
            int newz = EditingRoom.z;
            switch (direction)
            {
                case Direction.North:
                    newy = newy - 1;
                    break;
                case Direction.East:
                    newx = newx + 1;
                    break;
                case Direction.South:
                    newy = newy + 1;
                    break;
                case Direction.West:
                    newx = newx - 1;
                    break;
                case Direction.Up:
                    newz = newz + 1;
                    break;
                case Direction.Down:
                    newz = newz - 1;
                    break;
            }

            if (!drawWholeWorldCheckBox.Checked)
                drawMap(room.Area);
            else
                mapRoom(room, newx, newy, newz);
            var roomwrapper = (from r in rooms where r.Value.room == room select r.Value).FirstOrDefault();

            if (roomwrapper == null) return;
            var areanode = (from n in selectorTreeView.Nodes.OfType<TreeNode>() where n.Tag == roomwrapper.room.Area select n).FirstOrDefault();

            if (areanode != null)
            {
                var roomsnode = (from node in areanode.Nodes.OfType<TreeNode>() where node.Text == "Rooms" select node).FirstOrDefault();

                if (roomsnode != null)
                {
                    roomsnode.Nodes.Add(new TreeNode(roomwrapper.room.Vnum + " - " + roomwrapper.room.Name) { Tag = roomwrapper.room });
                }
            }

            selectNode(roomwrapper);
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
            pictureBox1.Image.Save("Map.jpg", ImageFormat.Jpeg);
            ////int width = mapPanel.Size.Width;
            ////int height = mapPanel.Size.Height;

            ////Bitmap bm = new Bitmap(width, height);
            ////mapPanel.DrawToBitmap(bm, new Rectangle(0, 0, width, height));
            //////var controls = new Control[mapPanel.Controls.Count];
            //////mapPanel.Controls.CopyTo(controls, 0);

            //////foreach (Control c in controls.Reverse())
            //////{

            //////    c.DrawToBitmap(bm, new Rectangle(c.Location.X, c.Location.Y, c.Width, c.Height));

            //////}
            ////bm.Save(@"Map.jpg", ImageFormat.Jpeg);
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
                    RoomData.Rooms[EditingRoom.vnum] = null;
                    EditingRoom.room.Area.Rooms[EditingRoom.vnum] = null;
                    EditingRoom.room.Vnum = vnum;
                    EditingRoom.room.Area.saved = false;
                    RoomData.Rooms[EditingRoom.vnum] = EditingRoom.room;
                    EditingRoom.room.Area.Rooms[EditingRoom.vnum] = EditingRoom.room;

                    var areanode = (from n in selectorTreeView.Nodes.OfType<TreeNode>() where n.Tag == EditingRoom.room.Area select n).FirstOrDefault();
                    if (areanode != null)
                    {
                        var roomsnode = (from node in areanode.Nodes.OfType<TreeNode>() where node.Text == "Rooms" select node).FirstOrDefault();

                        if (roomsnode != null)
                        {
                            var roomnode = (from r in roomsnode.Nodes.OfType<TreeNode>() where r.Tag == EditingRoom.room select r).FirstOrDefault();
                            if (roomnode != null)
                            {

                                roomnode.Text = EditingRoom.vnum + " - " + EditingRoom.room.Name;

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
            Drawer.Box box = Drawer.Boxes.Where((Drawer.Box b) => e2.X >= b.x + Drawer.Origin.X && e2.X <= b.x + Drawer.Origin.X + b.width && e2.Y >= b.y + Drawer.Origin.Y && e2.Y <= b.y + Drawer.Origin.Y + b.height).FirstOrDefault();
            if (box != null)
            {
                selectNode(box.wrapper);
            }
        }
    }
}