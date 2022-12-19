use std::error::Error;
use std::fs;
use std::io::{self, BufRead};
use std::path::Path;

// TODO: use proper errors instead of panic

/// Solves Day 7 from a file path `filename`
pub fn run<P>(filename: P) -> Result<Results, Box<dyn Error>>
where P: AsRef<Path> {
    let mut tree = Tree::new();
    let lines = read_lines(filename)?;
    for l in lines {
        let line = l?;
        tree.handle_line(&line)?;
    }

    Ok(Results::from_tree(tree)?)
}

/// Solves Day 7 from a string slice `input`
pub fn run_lines(input: &str) -> Result<Results, Box<dyn Error>> {
    let mut tree = Tree::new();
    for line in input.lines() {
        tree.handle_line(line)?;
    }

    Ok(Results::from_tree(tree)?)
}

#[derive(Debug)]
pub struct Results {
    sum_1: i32,
    size_2: i32
}

impl Results {
    const CAPACITY: i32 = 70_000_000;
    const MIN_UNUSED_SPACE: i32 = 30_000_000;

    fn from_tree(mut tree: Tree) -> Result<Results, Box<dyn Error>> {
        fn unused_space(total_size: i32, dir_size: i32) -> i32 {
            Results::CAPACITY - (total_size - dir_size)
        }

        let mut sum_1 = 0;
        let total_size = calc_directory_sizes(&mut tree.nodes)?;
        let mut smallest_dir_size_for_update = total_size;

        for node in tree.nodes.as_mut_slice() {
            match node {
                Node::Directory(dir) => {
                    let size = dir.get_size()?;

                    if size <= 100_000 {
                        sum_1 += size;
                    }

                    if unused_space(total_size, size) >= Results::MIN_UNUSED_SPACE &&
                        size < smallest_dir_size_for_update {
                        smallest_dir_size_for_update = size;
                    }
                },
                _ => continue
            };
        }

        Ok(Results { sum_1, size_2: smallest_dir_size_for_update })
    }
}

struct Tree {
    nodes: Vec<Node>,
    current_index: usize
}

impl Tree {
    const ROOT_INDEX: usize = 0;

    fn new() -> Tree {
        let root_dir = Directory::new("root", None);
        let root = Node::Directory(root_dir);
        let nodes = vec![root];
        Tree { nodes, current_index: Tree::ROOT_INDEX }
    }

    fn current_directory(&self) -> Result<&Directory, Box<dyn Error>> {
        let node = self.nodes.get(self.current_index).ok_or("bad index")?;

        match node {
            Node::Directory(dir) => Ok(dir),
            Node::File(_) => panic!("expected directory but found file")
        }
    }

    fn current_directory_mut(&mut self) -> Result<&mut Directory, Box<dyn Error>> {
        let node = self.nodes.get_mut(self.current_index).ok_or("bad index")?;

        match node {
            Node::Directory(dir) => Ok(dir),
            Node::File(_) => panic!("expected directory but found file")
        }
    }

    fn handle_line(&mut self, line: &str) -> Result<(), Box<dyn Error>> {
        let (first, rest) = line.split_once(' ').ok_or_else(|| format!("invalid line: {line}"))?;

        match first {
            "$" => self.handle_command(rest)?,
            "dir" => self.handle_dir(rest)?,
            _ => self.handle_file(first, rest)?
        }

        Ok(())
    }

    fn handle_command(&mut self, command: &str) -> Result<(), Box<dyn Error>> {
        let mut tokens = command.split(' ');
        let cmd = tokens.next().ok_or("invalid command")?;
        match cmd {
            "ls" => return Ok(()),

            "cd" => {
                let argument = tokens.next().ok_or_else(|| format!("missing argument for: {command}"))?;
                match argument {
                    "/" => self.current_index = 0,

                    ".." => {
                        let current_directory = self.current_directory()?;
                        self.current_index = current_directory.parent_index.ok_or("cannot cd .. from root directory")?;
                    },

                    _ => {
                        let current_directory = self.current_directory()?;
                        for child_index in &current_directory.children_indices {
                            match self.nodes.get(*child_index).ok_or("failed to get child")? {
                                Node::Directory(child_dir) if child_dir.name == argument => {
                                    self.current_index = *child_index;
                                    break;
                                },
                                _ => continue
                            };
                        }
                    }
                };
            },

            _ => panic!("invalid command: {command}")
        }

        Ok(())
    }

    fn handle_dir(&mut self, name: &str) -> Result<(), Box<dyn Error>> {
        let dir = Directory::new(name, Some(self.current_index));

        self.push(Node::Directory(dir))?;

        Ok(())
    }

    fn handle_file(&mut self, size_str: &str, name: &str) -> Result<(), Box<dyn Error>> {
        let size = size_str.parse::<i32>()?;
        let file = File { name: name.to_string(), size };

        self.push(Node::File(file))?;

        Ok(())
    }

    fn push(&mut self, node: Node) -> Result<(), Box<dyn Error>> {
        let index = self.nodes.len();
        self.nodes.push(node);
        let current_directory = self.current_directory_mut()?;
        current_directory.children_indices.push(index);

        Ok(())
    }
}

enum Node {
    Directory(Directory),
    File(File)
}

struct Directory {
    name: String,
    parent_index: Option<usize>,
    children_indices: Vec<usize>,
    size: Option<i32>
}

impl Directory {
    fn new(name: &str, parent_index: Option<usize>) -> Directory {
        Directory {
            name: name.to_string(),
            parent_index,
            children_indices: Vec::new(),
            size: None
        }
    }

    fn get_size(&self) -> Result<i32, Box<dyn Error>> {
        match self.size {
            Some(size) => Ok(size),
            None => panic!("size not initialized for {} - do you need to call calc_directory_sizes", self.name)
        }
    }
}

struct File {
    name: String,
    size: i32
}

// I'm not totally happy with this approach since it needs a temporary Vec, but it gets around the borrow checker
fn calc_directory_sizes(nodes: &mut [Node]) -> Result<i32, Box<dyn Error>> {
    let root_index = 0;
    let sizes: &mut Vec<Option<i32>> = &mut Vec::with_capacity(nodes.len());
    for _ in 0..nodes.len() {
        sizes.push(None);
    }

    let size = calc_directory_size_core(nodes, root_index, sizes)?;

    for i in 0..nodes.len() {
        match nodes.get_mut(i).ok_or_else(|| format!("failed to get node at {i}"))? {
            Node::Directory(dir) => {
                let dir_size = sizes[i].ok_or_else(|| format!("failed to get size for {}", dir.name))?;
                dir.size = Some(dir_size);
            },

            _ => continue
        }
    }

    Ok(size)
}

fn calc_directory_size_core(nodes: &[Node], index: usize, sizes: &mut [Option<i32>]) -> Result<i32, Box<dyn Error>> {
    if let Some(size) = sizes[index] {
        return Ok(size);
    }

    let mut size = 0;
    let node = nodes.get(index).ok_or_else(|| format!("failed to get node at {index}"))?;
    match node {
        Node::Directory(dir) => {
            for child_index in &dir.children_indices {
                let child = nodes.get(*child_index).ok_or_else(|| format!("failed to get node at {index}"))?;
                size += match child {
                    Node::Directory(_) => calc_directory_size_core(nodes, *child_index, sizes)?,
                    Node::File(child_file) => child_file.size
                };
            }
        },

        _ => panic!("unexpected node type at {index}")
    };

    sizes[index] = Some(size);
    Ok(size)
}

// from https://doc.rust-lang.org/rust-by-example/std_misc/file/read_lines.html
fn read_lines<P>(filename: P) -> io::Result<io::Lines<io::BufReader<fs::File>>>
where P: AsRef<Path> {
    let file = fs::File::open(filename)?;
    Ok(io::BufReader::new(file).lines())
}

#[cfg(test)]
mod tests {
    use crate::run_lines;

    const EXAMPLE: &'static str = "\
$ cd /
$ ls
dir a
14848514 b.txt
8504156 c.dat
dir d
$ cd a
$ ls
dir e
29116 f
2557 g
62596 h.lst
$ cd e
$ ls
584 i
$ cd ..
$ cd ..
$ cd d
$ ls
4060174 j
8033020 d.log
5626152 d.ext
7214296 k
";

    #[test]
    fn part_1() {
        let results = run_lines(EXAMPLE).unwrap();

        assert_eq!(results.sum_1, 95_437)
    }

    #[test]
    fn part_2() {
        let results = run_lines(EXAMPLE).unwrap();

        assert_eq!(results.size_2, 24_933_642)
    }
}
